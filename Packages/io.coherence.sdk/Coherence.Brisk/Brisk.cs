// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk
{
    using System;
    using System.Net;

    using Brook;
    using Brook.Octet;
    using Common;
    using Common.Pooling;
    using Connection;
    using Debugging;
    using Models;
    using Serializers;
    using Log;
    using ProtocolDef;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Tend.Client;
    using Tend.Models;
    using Transport;

    public partial class Brisk : IConnection
    {
        // Brisk defaults.
        public const ushort DefaultMTU = 1280;
        public const ushort MinMTU = 256; // arbitrary - basic connect request is 211
        public const ushort MaxMTU = ushort.MaxValue / 2; // arbitrary

        private const byte defaultSendFrequency = 20;

        private static readonly TimeSpan roundTripTimeThreshold = TimeSpan.FromSeconds(10);

        public event Action<ConnectResponse> OnConnect;
        public event Action<ConnectionCloseReason> OnDisconnect;
        public event Action<ConnectionException> OnError;
        public event Action<DeliveryInfo> OnDeliveryInfo;
        public event Action<ConnectionValidationRequest> OnValidateConnectionRequest;

        public bool CanSend => State == ConnectionState.Connected
                               && (tend?.CanSend ?? false)
                               && (transport?.CanSend ?? false)
                               && IsReadyToSendNextPacket();

        public Ping Ping => latencies?.Ping ?? default;
        public ClientID ClientID { get; private set; }
        public ushort ConnectionMTU => connectResponse.MTU;
        public byte SendFrequency { get; private set; } = defaultSendFrequency;
        public bool UseDebugStreams => Settings.UseDebugStreams;

        public ConnectionState State { get; private set; }
        public ConnectionSettings Settings { get; private set; }
        public uint InitialScene { get; set; }

        /// <inheritdoc/>
        public CustomPayload ValidatedHostPayload { get; private set; }

        /// <inheritdoc/>
        public CustomPayload ConnectionValidationPayload { get; set; }

        public string TransportDescription => transport?.Description;

        private ConnectResponse connectResponse;
        private readonly IStopwatch connectionTimer;
        private readonly Logger logger;
        private KeepAliveTimer keepAliveTimer;

        private TimeSpan nextSend;
        private readonly IStopwatch sendTimer;
        private OutPacket lastSentReliablePacket;
        private Dictionary<SequenceId, DateTime> pingSequence;

        private SendRateCounter sendRateCounter;
        private LatencyCollection latencies;
        private Tend tend;
        private ITransport transport;
        private Func<Logger, ITransport> transportFactory;
        private EndpointData endpoint;
        private ConnectionType connectionType;

        private readonly List<(IInOctetStream, IPEndPoint)> incomingBuffer = new List<(IInOctetStream, IPEndPoint)>();
        private readonly OobAckQueue oobAckQueue = new OobAckQueue();

        private readonly Pool<PooledOutOctetStream> oobStreamPool;
        private Pool<PooledOutOctetStream> streamPool;

        private BriskServices briskServices;

        public Brisk(Logger logger, BriskServices services = null)
        {
            briskServices = services ?? BriskServices.Default;
            this.logger = logger.With<Brisk>();

            sendTimer = services.SendTimerProvider();
            connectionTimer = services.ConnectionTimerProvider();
            transportFactory = services.TransportFactory;
            sendRateCounter = new SendRateCounter(logger);

            pingSequence = new Dictionary<SequenceId, DateTime>(Brook.SequenceId.MaxRange);

            // OOBs are all very small.
            oobStreamPool = Pool<PooledOutOctetStream>
                .Builder(pool => new PooledOutOctetStream(pool, DefaultMTU))
                .Concurrent()
                .Build();
        }

        public void Connect(EndpointData endpoint, ConnectionType connectionType, ConnectionSettings connectionSettings = null)
        {
            logger.Debug(nameof(Connect), ("host", endpoint.host), ("connectionType", connectionType));

            if (State != ConnectionState.Disconnected || transport != null)
            {
                HandleError(new ConnectionException("Already connecting/connected"));
                return;
            }

            Settings = connectionSettings ?? ConnectionSettings.Default;
            this.endpoint = endpoint;
            this.connectionType = connectionType;

            transport = transportFactory(logger);
            transport.OnOpen += InitializeAndBeginHandshake;
            transport.OnError += HandleError;

            tend = new Tend(logger);
            tend.OnDeliveryInfo += HandleDeliveryInfo;

            State = ConnectionState.Opening;

            transport.Open(endpoint, Settings);
        }

        public void Disconnect(ConnectionCloseReason connectionCloseReason, bool serverInitiated)
        {
            logger.Debug(nameof(Disconnect), ("reason", connectionCloseReason),
                ("serverInitiated", serverInitiated), ("state", State));

            if (State == ConnectionState.Disconnected)
            {
                return;
            }

            if (State != ConnectionState.Connected)
            {
                CleanUp();
                State = ConnectionState.Disconnected;
                return;
            }

            transport.PrepareDisconnect();

            if (!serverInitiated && transport.State == TransportState.Open)
            {
                try
                {
                    SendOOBMessage(new DisconnectRequest(connectionCloseReason, CustomPayload.Empty));
                }
                catch (Exception exception)
                {
                    logger.Warning(Warning.BriskFailedToSendDisconnect, ("exception", exception));
                }
            }

            CleanUp();
            State = ConnectionState.Disconnected;
            OnDisconnect?.Invoke(connectionCloseReason);
        }

        public void SendKickRequest(ClientID clientID, CustomPayload hostPayload)
        {
            logger.Debug(nameof(SendKickRequest), ("clientID", clientID));

            if (State != ConnectionState.Connected)
            {
                return;
            }

            if (connectionType != ConnectionType.Simulator)
            {
                logger.Error(Error.BriskClientCantKick, ("connectionType", connectionType));

                return;
            }

            SendOOBMessage(new KickRequest(clientID, hostPayload));
        }

        // It is important for 'Update' to be called after the 'OutConnection' sent it's data.
        // That's because 'Update' sends an ack packet whenever we're ready to send something.
        // If 'Update' was first, it would block 'OutConnection' from sending anything.
        public void Update()
        {
            if (State == ConnectionState.Disconnected)
            {
                return;
            }

            if (!IsReadyToSendNextPacket() || !transport.CanSend)
            {
                return;
            }

            bool shouldResendLastReliablePacket = !tend.CanSend;
            if (shouldResendLastReliablePacket)
            {
                logger.Trace($"Resending last reliable packet. Seq: {lastSentReliablePacket.SequenceId}");
                Send(lastSentReliablePacket);
                return;
            }

            switch (State)
            {
                case ConnectionState.Connected:
                    SendAckPacket();
                    break;

                case ConnectionState.Connecting:
                    SendConnectRequest();
                    break;
            }
        }

        public void Send(OutPacket packet)
        {
            TransportSend(packet, true);
        }

        public void Receive(List<InPacket> buffer)
        {
            if (State == ConnectionState.Disconnected)
            {
                return;
            }

            AssertTransportNotClosed();
            transport.Receive(incomingBuffer);

            foreach ((IInOctetStream stream, IPEndPoint from) in incomingBuffer)
            {
                // This handles case where we receive OOB disconnect packet,
                // which calls 'Disconnect'.
                // But also the case when the transport encountered an error
                // and closed the connection. If that happens, we could get null refs
                // in the following code because the CleanUp() method will have been called.
                if (State == ConnectionState.Disconnected)
                {
                    incomingBuffer.Clear();
                    return;
                }

                (InPacket packet, bool ok) = ProcessReceivedPacket(stream, from);
                if (!ok)
                {
                    continue;
                }

                if (State != ConnectionState.Connected)
                {
                    // If the State is not connected then we throw away all packets.
                    // The call to ProcessReceivedPacket above will catch the connection
                    // response and set connected if this is the right OOB.
                    //
                    // It's possible that the packet is for a previously bound client on the
                    // same port during tests.
                    continue;
                }

                if (!packet.IsOob)
                {
                    buffer.Add(packet);
                }
            }

            incomingBuffer.Clear();
        }

        public OutPacket CreatePacket(bool reliable) => CreatePacket(reliable, false);

        private void InitializeAndBeginHandshake()
        {
            logger.Debug(nameof(InitializeAndBeginHandshake), ("state", State));

            ClientID = default;
            nextSend = default;

            latencies = new LatencyCollection(Settings.Ping);

            oobAckQueue.Clear();

            connectionTimer.Restart();
            sendTimer.Restart();
            sendRateCounter.Reset();

            SendConnectRequest(true);
        }

        private void CleanUp()
        {
            StopKeepAlive();

            if (transport != null)
            {
                transport.OnOpen -= InitializeAndBeginHandshake;
                transport.OnError -= HandleError;

                AssertTransportNotClosed();
                transport.Close();

                transport = null;
            }

            if (tend != null)
            {
                tend.OnDeliveryInfo -= HandleDeliveryInfo;
                tend = null;
            }
        }

        private void UpdateNextSendTime(bool restartTimer = true)
        {
            TimeSpan elapsed = sendTimer.Elapsed;
            TimeSpan sendPeriod = TimeSpan.FromSeconds(1d / SendFrequency);

            if (elapsed.Ticks >= sendPeriod.Ticks * 2)
            {
                nextSend = TimeSpan.Zero;
            }
            else
            {
                TimeSpan overshoot = TimeSpan.FromTicks((elapsed - nextSend).Ticks % sendPeriod.Ticks);
                nextSend = sendPeriod - overshoot;
            }

            if (restartTimer)
            {
                sendTimer.Restart();
            }
        }

        private bool IsReadyToSendNextPacket()
        {
            return sendTimer.Elapsed >= nextSend;
        }

        private void TransportSend(OutPacket packet, bool isReliable, bool isMainThread = true)
        {
            DbgAssert.That(!isReliable || isMainThread, "Reliable packet sent from non-main thread");
            DbgAssert.ThatFmt(
                !isReliable || tend.IsValidSeqToSend(packet.SequenceId) ||
                // The lastSentReliable is being resent and it was the very last sequence ID.
                (packet.SequenceId.Equals(lastSentReliablePacket.SequenceId) && lastSentReliablePacket.SequenceId.Next().Equals(tend.OutgoingSequenceId)),
                "Sending invalid SEQ: S:{0}/O:{1}/R:{2}",
                packet.SequenceId,
                tend.OutgoingSequenceId,
                tend.LastReceivedByRemote);

            var stream = packet.Stream;

            var tport = transport;
            if (tport == null)
            {
                HandleError(new ConnectionException("Sending failed due to null transport"));
                return;
            }

            if (isReliable)
            {
                SetLastSentReliablePacket(packet);
                pingSequence[packet.SequenceId] = DateTime.UtcNow;
            }

            // Let tend know the packet is sent so it can increase the tend outgoing sequence for real.
            tend.OnPacketSent(packet.SequenceId, packet.IsReliable);

            UpdateNextSendTime();

            try
            {
                AssertTransportNotClosed(isMainThread);
                tport.Send(stream);

                sendRateCounter.Bump(SendFrequency, connectionTimer.Elapsed);
            }
            catch (Exception exception)
            {
                // Main thread exception are important thus rethrow
                if (isMainThread)
                {
                    throw;
                }

                // Non-main thread exception means keepalive failed, most likely due to
                // socket being closed by the time we tried to send. Safe to ignore.
                logger.Debug("Transport send exception", ("exception", exception));
            }
        }

        private void SetLastSentReliablePacket(in OutPacket packet)
        {
            // We have to copy the stream since the original stream is pooled and will be reused
            var stream = lastSentReliablePacket.Stream;

            // If no reliable packet was sent yet or we're in the process of resending last reliable packet
            if (stream == null || packet.Stream == stream)
            {
                if (packet.Stream == null)
                {
                    // Trying to catch https://app.zenhub.com/workspaces/engine-group-5fb3b64dabadec002057e6f2/issues/gh/coherence/engine/2263
                    logger.Warning(Warning.BriskOutPacketStreamNull);

                    stream = new OutOctetStream(Settings.Mtu);
                    lastSentReliablePacket = packet.WithStream(stream, logger);

                    // Sending a packet with an empty stream is non-sensical.
                    // Hopefully we never see this warning.
                    return;
                }
                else
                {
                    stream = new OutOctetStream((int)packet.Stream.Position);
                }
            }

            if (stream.Capacity < packet.Stream.Position)
            {
                stream = new OutOctetStream((int)packet.Stream.Position);
            }
            else
            {
                stream.Seek(0);
            }

            stream.WriteOctets(packet.Stream.Octets);

            lastSentReliablePacket = packet.WithStream(stream, logger);
        }

        private OutPacket CreatePacket(bool reliable, bool isOob)
        {
            var stream = isOob ? oobStreamPool.Rent() : streamPool.Rent();

            // Leave space for transport header
            stream.Seek((uint)transport.HeaderSize);

            var tendHeader = tend.WriteHeader(stream, reliable);
            WriteHeader(stream, isOob);

            return new OutPacket(stream, tendHeader.packetId, reliable, isOob, logger);
        }

        private (InPacket, bool) ProcessReceivedPacket(IInOctetStream stream, IPEndPoint from)
        {
            try
            {
                var valid = tend.ReadHeader(stream, out var header, out var didAck);
                if (!valid)
                {
                    return (default, false);
                }

                if (didAck)
                {
                    UpdatePingForSequenceId(header.receivedId);
                }

                var briskHeader = BriskHeader.Deserialize(stream);
                if (briskHeader.Mode == Mode.OobMode)
                {
                    IOobMessage oobMessage = BriskSerializer.DeserializeOobMessage(stream, ProtocolDef.Version.CurrentVersion);
                    ProcessOobMessage(oobMessage);
                }

                return (new InPacket(stream, header.packetId, header.isReliable, briskHeader.Mode == Mode.OobMode, from), true);
            }
            catch (Exception exception)
            {
                HandleError(new ConnectionException("Failed to deserialize packet headers", exception));
                return (default, false);
            }
        }

        private void ProcessOobMessage(IOobMessage oobMessage)
        {
            if (IsTraceLog(oobMessage.Type))
            {
                logger.Trace("Received oob", ("message", oobMessage));
            }
            else
            {
                logger.Debug("Received oob", ("message", oobMessage));
            }

            switch (oobMessage)
            {
                case ChangeSendFrequencyRequest changeSendFrequencyRequest:
                    if (State == ConnectionState.Connected)
                    {
                        SendFrequency = Math.Max((byte)1, changeSendFrequencyRequest.sendFrequency);
                        sendRateCounter.Reset();
                    }
                    break;
                case Ack _:
                case KeepAlive _:
                    break;
                case ConnectResponse connectResponse:
                    OnConnectResponse(connectResponse);
                    break;
                case DisconnectRequest disconnectRequest:
                    var exception = new ConnectionDeniedException(disconnectRequest.Reason, disconnectRequest.HostPayload);
                    HandleError(exception);
                    return;
                case ValidateConnectionRequest validateConnectionRequest:
                    HandleOnValidateConnectionRequest(validateConnectionRequest);
                    break;
                default:
                    logger.Warning(Warning.BriskUnknownOOB, ("message", oobMessage));
                    break;
            }
        }

        private void SendConnectRequest(bool isInitialRequest = false)
        {
            var connectInfo = new ConnectInfo(
                ProtocolDef.Version.CurrentVersion,
                endpoint.uniqueRoomId,
                endpoint.schemaId,
                endpoint.authToken,
                endpoint.roomSecret,
                connectionType == ConnectionType.Simulator,
                InitialScene,
                endpoint.rsVersion,
                Settings.Mtu,
                ConnectionValidationPayload
            );
            var connectRequest = new ConnectRequest(connectInfo);

            if (isInitialRequest)
            {
                State = ConnectionState.Connecting;
                logger.Debug("Initiating connect request", ("message", connectRequest));
            }

            SendOOBMessage(connectRequest);
        }

        private void OnConnectResponse(ConnectResponse response)
        {
            if (State != ConnectionState.Connecting)
            {
                return;
            }

            connectResponse = response;
            SendFrequency = response.SendFrequency;
            ClientID = response.ClientID;
            ValidatedHostPayload = response.HostPayload;

            // setup the stream pool with the expected MTU so we don't have
            // to pass the MTU around to the different serializers, they can just
            // fill until they're full.
            streamPool = Pool<PooledOutOctetStream>
                .Builder(pool => new PooledOutOctetStream(pool, response.MTU))
                .Concurrent()
                .Build();

            nextSend = TimeSpan.Zero;
            UpdateNextSendTime(false);

            ConnectionSuccess();
        }

        private void ConnectionSuccess()
        {
            logger.Debug("connection successful",
                ("clientID", connectResponse.ClientID),
                ("connectResponse", connectResponse));

            connectionTimer.Start();
            State = ConnectionState.Connected;
            OnConnect?.Invoke(connectResponse);

            StartKeepAlive();

            // Inform Tend that we can start handling reliable packets now that
            // there is a connection.
            tend.Connected = true;
        }

        private void HandleOnValidateConnectionRequest(ValidateConnectionRequest validateConnectionRequest)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            ValidateConnectionRespondDelegate respond = (ConnectionValidationResponse response) =>
            {
                if (State != ConnectionState.Connected)
                {
                    return;
                }

                var responseOOB = new ValidateConnectionResponse(validateConnectionRequest.ClientID, response.Accepted, response.HostPayload);
                SendOOBMessage(responseOOB);
            };

            if (OnValidateConnectionRequest != null)
            {
                OnValidateConnectionRequest.Invoke(new ConnectionValidationRequest(
                    validateConnectionRequest.ClientID,
                    validateConnectionRequest.UserPayload,
                    respond));
            }
            else
            {
                // In case there's no handler, we accept the connection by default.
                respond(new ConnectionValidationResponse(true));
            }
        }

        private void SendOOBMessage(IOobMessage oobMessage, bool isMainThread = true)
        {
            try
            {
                OutPacket packet = CreatePacket(oobMessage.IsReliable, true);
                BriskSerializer.SerializeOobMessage(packet.Stream, oobMessage, ProtocolDef.Version.CurrentVersion);

                if (IsTraceLog(oobMessage.Type))
                {
                    logger.Trace("Sending oob", ("message", oobMessage));
                }
                else
                {
                    logger.Debug("Sending oob", ("message", oobMessage));
                }

                if (oobMessage.IsReliable)
                {
                    oobAckQueue.Enqueue(oobMessage, packet.SequenceId);
                }

                TransportSend(packet, oobMessage.IsReliable, isMainThread);
            }
            catch (Exception exception)
            {
                // Main thread exception are important thus rethrow
                if (isMainThread)
                {
                    throw;
                }

                // Non-main thread exception means keepalive failed, most likely due to
                // socket being closed by the time we tried to send. Safe to ignore.
                logger.Debug("OOB send failure", ("exception", exception));
            }
        }

        private void WriteHeader(IOutOctetStream stream, bool isOob)
        {
            // Create header & serialize into stream
            var header = new BriskHeader(isOob ? Mode.OobMode : Mode.NormalMode);
            header.Serialize(stream);
        }

        // Used to send a packet to ack received packets if we haven't sent a packet in some time.
        // This ensures that acks are sent if no more data is but is not connection keep alive mechanism.
        // Note that we send ack regardless if we received something since last ack or not. The reason
        // for this is that we don't know if previous ack has reached the replication server. This can
        // create a situation in which neither side sends anything assuming that the other side has
        // received everything sent so far.
        private void SendAckPacket()
        {
            SendOOBMessage(Ack.Instance);
        }

        private void HandleError(ConnectionException exception)
        {
            logger.Debug(nameof(HandleError), ("exception", exception), ("state", State));
            OnError?.Invoke(exception);
        }

        private void HandleDeliveryInfo(DeliveryInfo info)
        {
            PacketStatus packetStatus = oobAckQueue.Ack(info, out IOobMessage message);

            logger.Trace(nameof(HandleDeliveryInfo), ("ackStatus", packetStatus),
                ("seq", info.PacketSequenceId.Value), ("delivered", info.WasDelivered));

            switch (packetStatus)
            {
                case PacketStatus.Unknown:
                    OnDeliveryInfo?.Invoke(info);
                    break;

                case PacketStatus.Lost:
                    // There's no reason to resend those acks since the next
                    // reliable packet/ack will deliver new receive mask.
                    if (message.Type != OobMessageType.Ack)
                    {
                        SendOOBMessage(message);
                    }
                    break;

                case PacketStatus.Acked:
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(PacketStatus));
            }
        }

        private void UpdatePingForSequenceId(SequenceId sequenceId)
        {
            if (pingSequence.TryGetValue(sequenceId, out var time))
            {
                var roundTripTime = DateTime.UtcNow - time;

                // Discard unrealistic samples
                if (roundTripTime >= TimeSpan.Zero && roundTripTime <= roundTripTimeThreshold)
                {
                    var latency = (ushort)(roundTripTime.TotalMilliseconds / 2);
                    latencies.AddLatency(latency);
                }
            }
            else
            {
                logger.Error(Error.BriskPingSequenceMissing, ("sequence", sequenceId));
            }
        }

        private void StartKeepAlive()
        {
            if (briskServices.KeepAliveProvider())
            {
                keepAliveTimer ??= new KeepAliveTimer(this);

                keepAliveTimer.StartKeepAlive();
            }
        }

        private void StopKeepAlive()
        {
            keepAliveTimer?.StopKeepAlive();
        }

        [Conditional("DEBUG")]
        private void AssertTransportNotClosed(bool isMainThread = true, [CallerMemberName] string caller = null)
        {
            if (isMainThread && transport?.State == TransportState.Closed)
            {
                logger.Error(Error.BriskUnexpectedAccessOfClosedTransport,
                    ("caller", caller),
                    ("transport", TransportDescription));
            }
        }

        private static bool IsTraceLog(OobMessageType messageType)
        {
            switch (messageType)
            {
                case OobMessageType.Ack:
                case OobMessageType.KeepAlive:
                case OobMessageType.ConnectRequest:
                case OobMessageType.ConnectResponse:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Response to a connection validation request.
    /// </summary>
    public struct ConnectionValidationResponse
    {
        internal bool Accepted;
        internal CustomPayload HostPayload;

        /// <param name="accepted">Boolean stating if the new connection should be accepted.</param>
        /// <param name="hostPayload">Optional custom payload sent to the new connection.</param>
        public ConnectionValidationResponse(bool accepted, CustomPayload hostPayload = default)
        {
            Accepted = accepted;
            HostPayload = hostPayload;
        }
    }

    internal delegate void ValidateConnectionRespondDelegate(ConnectionValidationResponse response);

    /// <summary>
    /// Request for validation of a pending client connection.
    /// </summary>
    [Serializable]
    public struct ConnectionValidationRequest
    {
        /// <summary>
        /// The ClientID of the new client connection waiting for validation.
        /// </summary>
        public ClientID ClientID { get; }

        /// <summary>
        /// The optional custom payload sent by the new client connection.
        /// </summary>
        /// <remarks>This can contain any payload used for validation, such as access tokens or similar.</remarks>
        public CustomPayload UserPayload { get; }

        private ValidateConnectionRespondDelegate respondDelegate { get; }

        internal ConnectionValidationRequest(ClientID clientID, CustomPayload userPayload, ValidateConnectionRespondDelegate respond)
        {
            ClientID = clientID;
            UserPayload = userPayload;
            respondDelegate = respond;
        }

        /// <summary>
        /// Responds to the connection validation request.
        /// </summary>
        /// <remarks>
        /// Call this only once.
        /// This can be called asynchronously.
        /// </remarks>
        public readonly void Respond(ConnectionValidationResponse response) => respondDelegate(response);
    }
}
