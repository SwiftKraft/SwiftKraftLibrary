// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using System;
    using Brook;
    using Brook.Octet;
    using Common;
    using Connection;
    using Log;
    using Web;
    using Stats;
    using System.Collections.Generic;
    using System.Net;
    using Common.Pooling;

    internal class WebTransport : ITransport
    {
        public event Action OnOpen;
        public event Action<ConnectionException> OnError;

        public TransportState State { get; private set; }
        public bool IsReliable => true;
        public bool CanSend => true;
        public int HeaderSize => 0;
        public string Description => "Web";

        // Create an interface for the WebInterop so we're not accessing it directly, and can
        // moq it.
        private readonly Action<int, string, int, string, string, string, string, string> WebConnect;
        private readonly Action<int> WebDisconnect;
        private readonly Action<int, byte[], int> WebSend;

        private readonly Queue<byte[]> receiveQueue = new();

        private readonly int interopId = -1;
        private readonly IStats stats;
        private readonly Logger logger;

        private readonly Pool<PooledInOctetStream> streamPool;

        public WebTransport(
            Func<Action, Action<byte[]>, Action<JsError>, int> initializeConnection,
            Action<int, string, int, string, string, string, string, string> connect,
            Action<int> disconnect,
            Action<int, byte[], int> send,
            IStats stats,
            Logger logger)
        {
#if !UNITY_WEBGL
            logger.Error(Error.WebTransportNotSupported);
#endif

            WebConnect = connect;
            WebDisconnect = disconnect;
            WebSend = send;

            interopId = initializeConnection(OnChannelOpen, OnPacket, OnJSError);

            streamPool = Pool<PooledInOctetStream>
                .Builder(pool => new PooledInOctetStream(pool))
                .Build();

            this.stats = stats;
            this.logger = logger;
        }

        public void Open(EndpointData data, ConnectionSettings _)
        {
            State = TransportState.Opening;
            WebConnect(
                interopId,
                data.GetHostAndPort(),
                data.roomId,
                data.runtimeKey,
                data.uniqueRoomId.ToString(),
                data.WorldIdString(),
                data.region,
                data.schemaId
            );
        }

        public void Close()
        {
            State = TransportState.Closed;
            WebDisconnect(interopId);
        }

        public void Send(IOutOctetStream stream)
        {
            var packet = stream.Close();
            WebSend(interopId, packet.Array, packet.Count);
            stats.TrackOutgoingPacket(stream.Position);
            stream.ReturnIfPoolable();
        }

        public void Receive(List<(IInOctetStream, IPEndPoint)> buffer)
        {
            while (receiveQueue.Count > 0)
            {
                var data = receiveQueue.Dequeue();

                var packet = streamPool.Rent();
                packet.Reset(data);

                stats.TrackIncomingPacket((uint)packet.RemainingOctetCount);
                buffer.Add((packet, null));
            }
        }

        private void OnChannelOpen()
        {
            if (State == TransportState.Opening)
            {
                State = TransportState.Open;
                OnOpen?.Invoke();
            }
        }

        private void OnPacket(byte[] data)
        {
            receiveQueue.Enqueue(data);
        }

        private void OnJSError(JsError error)
        {
            if (State == TransportState.Closed)
            {
                return;
            }

            switch (error.ErrorType)
            {
                case ErrorType.OfferError:
                    switch (error.ErrorResponse.ErrorCode)
                    {
                        case ErrorCode.InvalidChallenge:
                            OnError?.Invoke(new ConnectionDeniedException(ConnectionCloseReason.InvalidChallenge, CustomPayload.Empty, error.ErrorMessage));
                            break;

                        case ErrorCode.RoomNotFound:
                            OnError?.Invoke(new ConnectionDeniedException(ConnectionCloseReason.RoomNotFound, CustomPayload.Empty, error.ErrorMessage));
                            break;

                        case ErrorCode.RoomFull:
                            OnError?.Invoke(new ConnectionDeniedException(ConnectionCloseReason.RoomFull, CustomPayload.Empty, error.ErrorMessage));
                            break;

                        default:
                            var reason = (error.StatusCode >= 400 && error.StatusCode <= 499)
                                ? ConnectionCloseReason.InvalidChallenge
                                : ConnectionCloseReason.ServerError;
                            OnError?.Invoke(new ConnectionDeniedException(reason, CustomPayload.Empty, $"StatusCode: {error.StatusCode}"));
                            break;
                    }
                    break;

                case ErrorType.ChannelError:
                    OnError?.Invoke(new ConnectionException($"Channel error: {error.ErrorMessage}"));
                    break;

                default:
                    OnError?.Invoke(new ConnectionException(error.ErrorMessage));
                    break;
            }
        }

        public void PrepareDisconnect() { }
    }
}
