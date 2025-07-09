// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using Brook;
    using Brook.Octet;
    using Common;
    using Common.Pooling;
    using Connection;
    using Debugging;
    using Log;
    using Stats;

    public class UdpTransportV2 : IListenTransport
    {
        struct ReceiveEvent
        {
            public ArraySegment<byte> Data;
            public ConnectionException Error;
            public IPEndPoint From;
        }

        public const int HeaderSizeBytes = UdpTransport.HeaderSizeBytes;
        private static readonly IPEndPoint AnyEndpoint = new(IPAddress.Any, 0);

        public event Action OnOpen;
        public event Action<ConnectionException> OnError;

        public TransportState State { get; private set; }
        public bool IsReliable => false;
        public bool CanSend => State == TransportState.Open;
        public int HeaderSize => HeaderSizeBytes;
        public string Description => "UDPv2";

        private bool IsInListenMode => remoteEndPoint == null;
        private bool IsInClientMode => !IsInListenMode;

        private Socket socket;
        private IPEndPoint remoteEndPoint;
        private readonly IStats stats;
        private readonly Logger logger;

        private SessionID sessionId;
        private ushort roomId;
        private ushort maxBufferSize;

        private readonly Timeout timeout;

        private readonly Pool<byte[]> bufferPool;
        private readonly Pool<PooledInOctetStream> streamPool;
        private readonly ConcurrentQueue<ReceiveEvent> receiveQueue;

        public void PrepareDisconnect() { }

        public UdpTransportV2(ushort maxBufferSize, IStats stats, Logger logger, IDateTimeProvider dateTimeProvider = null)
        {
            this.stats = stats;
            this.logger = logger.With<UdpTransportV2>();
            this.maxBufferSize = maxBufferSize;

            bufferPool = Pool<byte[]>.Builder(_ => new byte[maxBufferSize]).Concurrent().Build();
            streamPool = Pool<PooledInOctetStream>.Builder(pool => new PooledInOctetStream(pool, maxBufferSize)).Build();
            receiveQueue = new ConcurrentQueue<ReceiveEvent>();
            timeout = new Timeout(dateTimeProvider ?? new SystemDateTimeProvider(), HandleTimeout);
        }

        public void Open(EndpointData endpoint, ConnectionSettings settings)
        {
            logger.Debug("Open",
                ("endpoint", endpoint),
                ("mtu", settings.Mtu),
                ("maxBufferSize", maxBufferSize));

            try
            {
                remoteEndPoint = GetIPEndPoint(endpoint);
                roomId = endpoint.roomId;

                socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(remoteEndPoint);

                timeout.SetTimeout(settings.DisconnectTimeout);

                StartReceiving();
            }
            catch (Exception exception)
            {
                OnError?.Invoke(new ConnectionException("Open failed", exception));
                return;
            }

            State = TransportState.Open;
            OnOpen?.Invoke();
        }

        public void Listen(EndpointData endpoint, ConnectionSettings settings)
        {
            logger.Debug("Listen",
                ("endpoint", endpoint),
                ("mtu", settings.Mtu),
                ("maxBufferSize", maxBufferSize));

            try
            {
                roomId = endpoint.roomId;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(GetIPEndPoint(endpoint));

                StartReceiving();
            }
            catch (Exception exception)
            {
                OnError?.Invoke(new ConnectionException("Listen failed", exception));
                return;
            }

            State = TransportState.Open;
            OnOpen?.Invoke();
        }

        public void Close()
        {
            Close(true);
        }

        private void Close(bool raiseError)
        {
            logger.Debug("Close");

            State = TransportState.Closed;

            try
            {
                socket?.Close();
                socket?.Dispose();
            }
            catch (Exception exception)
            {
                if (raiseError)
                {
                    OnError?.Invoke(new ConnectionException("Close failed", exception));
                }
            }
        }

        private void StartReceiving()
        {
            var socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.RemoteEndPoint = IsInClientMode ? remoteEndPoint : AnyEndpoint;
            socketEventArgs.Completed += (_, args) => DataReceived(args);

            var buffer = bufferPool.Rent();
            socketEventArgs.SetBuffer(buffer, 0, buffer.Length);

            Receive(socketEventArgs);
        }

        private void Receive(SocketAsyncEventArgs args)
        {
            try
            {
                if (IsInClientMode)
                {
                    if (!socket.ReceiveAsync(args))
                    {
                        DataReceived(args);
                    }
                }
                else // Listen mode
                {
                    if (!socket.ReceiveFromAsync(args))
                    {
                        DataReceived(args);
                    }
                }
            }
            catch (ObjectDisposedException) // only catch this, bubble everything else up
            {
                // do nothing, socket is closed now
                // socket was closed while asyncreceive was waiting
            }
        }

        private void DataReceived(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                if (IsInListenMode && args.SocketError == SocketError.ConnectionReset)
                {
                    logger.Debug("Connection reset", ("addr", args.RemoteEndPoint));
                }
                else
                {
                    receiveQueue.Enqueue(new ReceiveEvent
                    {
                        Error = new ConnectionException($"Receive error: {args.SocketError}, From: {args.RemoteEndPoint}")
                    });
                    return;
                }
            }

            stats?.TrackIncomingPacket((uint)args.BytesTransferred);

            var receivedData = new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred);
            var fromEndpoint = args.RemoteEndPoint as IPEndPoint;

            DbgAssert.That(receivedData.Array != null, "Expected non-null buffer");
            DbgAssert.That(receivedData.Offset == 0, "Expected offset to be 0");
            DbgAssert.That(IsInClientMode || fromEndpoint != null, $"Expected fromEndpoint to be set");

            logger.Trace("DataReceived", ("mode", IsInClientMode ? "Client" : "Listen"),
                ("from", fromEndpoint), ("data", receivedData.Count));

            if (IsInListenMode || remoteEndPoint.Equals(fromEndpoint))
            {
                var buffer = bufferPool.Rent();
                args.SetBuffer(buffer, 0, buffer.Length);

                receiveQueue.Enqueue(new ReceiveEvent
                {
                    Data = receivedData,
                    From = fromEndpoint,
                });
            }
            else
            {
                // Reuse the buffer for the next receive
                args.SetBuffer(receivedData.Array, receivedData.Offset, receivedData.Count);

                logger.Warning(
                    Warning.UDPReceivedFromInvalidIP,
                    ("expected", remoteEndPoint),
                    ("actual", fromEndpoint));
            }

            // Issue next receive
            if (IsInListenMode)
            {
                args.RemoteEndPoint = AnyEndpoint;
            }

            Receive(args);
        }

        public void Send(IOutOctetStream stream)
            => Send(stream, sessionId);

        public void SendTo(IOutOctetStream stream, IPEndPoint endpoint, SessionID sessionID)
            => Send(stream, sessionID, endpoint);

        private void Send(IOutOctetStream stream, SessionID sessionID, IPEndPoint endpoint = null)
        {
            logger.Trace("Send", ("mode", IsInClientMode ? "Client" : "Listen"),
                ("sessionID", sessionID), ("to", endpoint ?? remoteEndPoint),
                ("data", stream.Position));

            WriteHeader(stream, sessionID);

            var data = stream.Close();
            stats?.TrackOutgoingPacket((uint)data.Count);

            if (IsInClientMode)
            {
                socket.Send(data);
            }
            else // Listen mode
            {
                // On optimization: both `SendTo` and `SendToAsync` allocates, with former doing 1 alloc and latter
                // doing 2 allocs. `SendPacketsAsync` is not implemented. Thus it is impossible to avoid allocations
                // in Unity.
                socket.SendTo(data.Array!, data.Offset, data.Count, SocketFlags.None, endpoint!);
            }

            stream.ReturnIfPoolable();
        }

        private void WriteHeader(IOutOctetStream stream, SessionID sessionID)
        {
            var streamEnd = stream.Position;
            stream.Seek(0);

            stream.WriteUint16(roomId);
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

            var anyValidPacketReceived = false;
            while (receiveQueue.TryDequeue(out var receiveEvent))
            {
                if (receiveEvent.Error != null)
                {
                    OnError?.Invoke(receiveEvent.Error);
                    return;
                }

                var stream = streamPool.Rent();
                stream.Reset(receiveEvent.Data);
                bufferPool.Return(receiveEvent.Data.Array);

                try
                {
                    if (!HandleRoomId(stream))
                    {
                        stream.Return();
                        continue;
                    }

                    // In Listen mode it is the responsibility of the caller to handle the sessionID
                    if (IsInClientMode && !HandleSessionId(stream))
                    {
                        stream.Return();
                        continue;
                    }
                }
                catch (Exception exception)
                {
                    stream.Return();
                    OnError?.Invoke(new ConnectionException("Failed to read roomID/sessionID", exception));
                    return;
                }

                anyValidPacketReceived = true;
                buffer.Add((stream, receiveEvent.From));
            }

            if (IsInClientMode)
            {
                timeout.Check(anyValidPacketReceived);
            }
        }

        private bool HandleRoomId(InOctetStream stream)
        {
            if (stream.RemainingOctetCount < Flux.Flux.roomByteCount)
            {
                return false;
            }

            var packetRoomId = stream.ReadUint16();
            if (packetRoomId == this.roomId)
            {
                return true;
            }

            logger.Warning(
                Warning.UDPWrongRoomID,
                ("expected", this.roomId),
                ("actual", packetRoomId));
            return false;
        }

        private bool HandleSessionId(InOctetStream stream)
        {
            if (stream.RemainingOctetCount < SessionID.Size)
            {
                return false;
            }

            var packetSessionId = SessionID.Read(stream);
            if (sessionId == SessionID.None)
            {
                sessionId = packetSessionId;
                logger.Debug("SessionID set", ("sessionID", sessionId));
                return true;
            }

            if (sessionId == packetSessionId)
            {
                return true;
            }

            logger.Debug(
                "Packet with wrong sessionID",
                ("expected", sessionId),
                ("actual", packetSessionId));
            return false;
        }

        private void HandleTimeout(ConnectionTimeoutException timeoutException)
        {
            OnError?.Invoke(timeoutException);
        }

        private static IPEndPoint GetIPEndPoint(in EndpointData endpoint)
        {
            return new IPEndPoint(IPAddress.Parse(endpoint.host), endpoint.port);
        }
    }
}
