// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook;
    using Common;
    using Connection;
    using Log;
    using Stats;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Pooling;

    internal class TcpTransport : ITransport
    {
        public event Action OnOpen;
        public event Action<ConnectionException> OnError;

        public TransportState State { get; private set; }
        public bool CanSend => State == TransportState.Open;
        public bool IsReliable => true;
        public int HeaderSize => (int)TcpTransportLoop.HEADER_SIZE;
        public string Description => "TCP";

        private readonly Logger logger;
        private readonly IStats stats;
        private ConnectionSettings settings;

        private IPEndPoint originalRemoteEndpoint;
        private CancellationTokenSource cancellationSource;

        private ConcurrentQueue<(byte[], ConnectionException)> receiveQueue;
        private AsyncQueue<IOutOctetStream> sendQueue;
        private readonly Pool<PooledInOctetStream> streamPool;

        public TcpTransport(IStats stats, Logger logger)
        {
            this.stats = stats;
            this.logger = logger.With(typeof(TcpTransport));

            streamPool = Pool<PooledInOctetStream>
                .Builder(pool => new PooledInOctetStream(pool))
                .Build();
        }

        public void Open(EndpointData endpoint, ConnectionSettings settings)
        {
            logger.Debug($"Opening", ("host", endpoint.host), ("port", endpoint.port));

            if (State != TransportState.Closed)
            {
                logger.Warning(Warning.TCPTranportInvalidState, ("currentState", State));
                return;
            }

            originalRemoteEndpoint = GetEndpoint(endpoint);
            this.settings = settings;

            var client = new TcpClient(new IPEndPoint(0, 0));
            client.ReceiveTimeout = (int)settings.DisconnectTimeout.TotalMilliseconds;
            client.NoDelay = true;

            receiveQueue = new();
            sendQueue = new();
            cancellationSource = new();

            State = TransportState.Opening;

            ConnectClient(client, endpoint).Then(task =>
            {
                if (task.IsFaulted && !cancellationSource.Token.IsCancellationRequested)
                {
                    var exception = task.Exception?.InnerException ?? task.Exception;
                    logger.Debug($"Connection failed", ("exception", exception));
                    OnError?.Invoke(GetConnectionException("TCP connection failure", exception));
                }

                client.Dispose();
            });
        }

        public void Close()
        {
            logger.Debug("Closing", ("state", State));

            State = TransportState.Closed;

            cancellationSource?.Cancel();
        }

        private async Task ConnectClient(TcpClient client, EndpointData endpoint)
        {
            await client.ConnectAsync(endpoint.host, endpoint.port);

            // NOTE: This continuation runs on a main thread

            if (cancellationSource.Token.IsCancellationRequested)
            {
                return;
            }

            if (!client.Connected)
            {
                var exception = new Exception("Client not connected");
                logger.Debug($"Connection failed", ("exception", exception));
                OnError?.Invoke(GetConnectionException("TCP connection failure", exception));
                return;
            }

            logger.Debug("Open", ("localEp", client.Client.LocalEndPoint));

            var transportLoop = new TcpTransportLoop(
                client.GetStream(),
                endpoint.uniqueRoomId,
                receiveQueue,
                sendQueue,
                stats,
                logger,
                cancellationSource.Token);

            State = TransportState.Open;
            OnOpen?.Invoke();
            await transportLoop.Run();
        }

        public void Send(IOutOctetStream data)
        {
            if (State != TransportState.Open)
            {
                return;
            }

            sendQueue.Enqueue(data);
        }

        public void Receive(List<(IInOctetStream, IPEndPoint)> buffer)
        {
            if (State != TransportState.Open)
            {
                return;
            }

            while (receiveQueue.TryDequeue(out (byte[] data, ConnectionException exception) tuple))
            {
                if (HandleReceiveException(tuple.exception))
                {
                    return;
                }

                var packet = streamPool.Rent();
                packet.Reset(tuple.data);

                buffer.Add((packet, originalRemoteEndpoint));
            }
        }

        private bool HandleReceiveException(ConnectionException exception)
        {
            if (exception == null)
            {
                return false;
            }

            exception = GetConnectionException(exception.Message, exception.InnerException);

            logger.Debug($"Encountered exception", ("exception", exception));
            OnError?.Invoke(exception);
            return true;
        }

        private ConnectionException GetConnectionException(string message, Exception innerException)
        {
            // The exception can be an IO.Exception but the socket was connection reset or shutdown.
            // in these cases, we want the innerException.InnerException.
            var socketException = innerException as SocketException ?? innerException?.InnerException as SocketException;
            if (socketException != null)
            {
                if (socketException.SocketErrorCode == SocketError.TimedOut)
                {
                    return new ConnectionTimeoutException(settings.DisconnectTimeout, message, socketException);
                }
                else if (socketException.SocketErrorCode == SocketError.ConnectionReset ||
                      socketException.SocketErrorCode == SocketError.Shutdown ||
                      socketException.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    return new ConnectionClosedException(message, socketException);
                }
            }

            return new ConnectionException(message, innerException);
        }

        private IPEndPoint GetEndpoint(EndpointData endpointData)
        {
            if (IPAddress.TryParse(endpointData.host, out IPAddress address))
            {
                return new IPEndPoint(address, endpointData.port);
            }

            throw new Exception($"Failed to parse host: {endpointData.host}");
        }

        public void PrepareDisconnect() { }
    }
}
