// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using System;
    using System.Collections.Concurrent;
    using Flux;
    using Brook;
    using Common;
    using Connection;
    using Log;
    using Stats;
    using System.Collections.Generic;
    using UdpClient = Flux.UdpClient;
    using System.Net;
    using Debugging;

    public class UdpTransport : ITransport
    {
        public const int HeaderSizeBytes = Flux.roomByteCount + SessionID.Size;

        public event Action OnOpen;
        public event Action<ConnectionException> OnError;

        public TransportState State { get; protected set; }
        public bool IsReliable => false;
        public bool CanSend => State == TransportState.Open;
        public int HeaderSize => HeaderSizeBytes;
        public string Description => "UDP";

        protected readonly Flux flux;
        protected readonly Logger logger;
        protected readonly IDateTimeProvider dateTimeProvider;

        protected DateTime lastValidPacketTime;
        protected ConnectionSettings settings;
        protected bool dev_blockTraffic;

        private readonly IStats stats;
        private readonly UdpClient port;

        private readonly ConcurrentQueue<(IInOctetStream stream, IPEndPoint from)> receiveQueue = new();
        private SessionID sessionID;

        public UdpTransport(IStats stats, Logger logger, IDateTimeProvider dateTimeProvider = null)
        {
            this.stats = stats;
            this.logger = logger.With<UdpTransport>();
            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();

            port = new UdpClient(logger);
            flux = new Flux(port, logger);

            flux.OnPacketReceived += OnPacket;

            if (stats != null)
            {
                flux.OnPacketSent += stats.TrackOutgoingPacket;
            }

            DevSetup();
        }

        private void DevSetup()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg == "--dev-blockudp")
                {
                    dev_blockTraffic = true;
                    return;
                }
            }
        }

        public void Open(EndpointData endpoint, ConnectionSettings settings)
        {
            logger.Debug($"Opening", ("host", endpoint.host), ("port", endpoint.port));

            try
            {
                flux.SetRoomId(endpoint.roomId);
                flux.Open($"{endpoint.host}:{endpoint.port}");
            }
            catch (Exception exception)
            {
                RaiseOnError(new ConnectionException("Failed to open UDP transport", exception));
                return;
            }

            State = TransportState.Open;
            this.settings = settings;
            lastValidPacketTime = dateTimeProvider.UtcNow;

            RaiseOnOpen();
        }

        public void Close()
        {
            State = TransportState.Closed;

            port.Close();
            receiveQueue.Clear();
        }

        public void Send(IOutOctetStream stream)
        {
            if (dev_blockTraffic)
            {
                return;
            }

            WriteHeaderWithSpaceForRoomID(stream);

            var data = stream.Close();
            flux.SendPacket(data);

            stream.ReturnIfPoolable();
        }

        private void WriteHeaderWithSpaceForRoomID(IOutOctetStream stream)
        {
            var streamEnd = stream.Position;
            stream.Seek(Flux.roomByteCount);

            SessionID.Write(sessionID, stream);

            DbgAssert.ThatFmt(stream.Position == HeaderSize,
                "Header size mismatch, was: {}, expected: {}",
                stream.Position, HeaderSize);

            stream.Seek(streamEnd);
        }

        public void Receive(List<(IInOctetStream, IPEndPoint)> buffer)
        {
            if (State == TransportState.Closed)
            {
                logger.Debug("DBG_ERROR: Receive in the closed state");
                return;
            }

            bool anyValidPacket = false;
            while (receiveQueue.TryDequeue(out var packet))
            {
                if (stats != null)
                {
                    stats.TrackIncomingPacket((uint)packet.stream.RemainingOctetCount);
                }

                if (HandleSessionID(packet.stream))
                {
                    anyValidPacket = true;
                    buffer.Add(packet);
                }
            }

            CheckForTimeout(anyValidPacket);
        }

        public void PrepareDisconnect() { }

        protected void RaiseOnOpen()
        {
            OnOpen?.Invoke();
        }

        protected void RaiseOnError(ConnectionException exception)
        {
            OnError?.Invoke(exception);
        }

        protected virtual void CheckForTimeout(bool anyValidPacketReceived)
        {
            if (anyValidPacketReceived)
            {
                lastValidPacketTime = dateTimeProvider.UtcNow;
                return;
            }

            if (dateTimeProvider.UtcNow - lastValidPacketTime >= settings.DisconnectTimeout)
            {
                var exception = new ConnectionTimeoutException(
                    settings.DisconnectTimeout,
                    "Connection timeout: no valid message received in time");
                OnError?.Invoke(exception);
            }
        }

        protected virtual bool HandleSessionID(IInOctetStream stream)
        {
            SessionID packetSessionID = (SessionID)(stream.ReadUint8() | stream.ReadUint8() << 8);
            if (sessionID == SessionID.None)
            {
                sessionID = packetSessionID;
                return true;
            }

            if (sessionID != packetSessionID)
            {
                logger.Debug($"Packet with wrong sessionID. Expected: {sessionID}, Got: {packetSessionID}");
                return false;
            }

            return true;
        }

        private void OnPacket(IInOctetStream stream, IPEndPoint receivedFrom)
        {
            receiveQueue.Enqueue((stream, receivedFrom));
        }
    }
}
