// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Log;
    using Utils;

    public class DotNetWebSocket : IWebSocket
    {
        public bool PingWebSocket => false;

        private event Action OnConnect;
        private event Action OnDisconnect;
        private event Action<Error, string> OnWebSocketFail;
        private event Action<string> OnReceive;
        private event Action<int, string, Error, string> OnSendFail;

        private ClientWebSocket ws;
        private CancellationTokenSource abortTokenSource;
        private CancellationToken abortToken;
        private Task receiveTask;
        private Task connectTask;
        private Task sendTask = Task.CompletedTask;
        private readonly Logger logger = Log.GetLogger<DotNetWebSocket>();

        public bool IsConnected() => ws?.State == WebSocketState.Open;

        public void OpenSocket(string endpoint, Action onConnect, Action onDisconnect,
            Action<string> onReceive, Action<Error, string> onError, Action<int, string, Error, string> onSendFail)
        {
            OnConnect = onConnect;
            OnDisconnect = onDisconnect;
            OnWebSocketFail = onError;
            OnReceive = onReceive;
            OnSendFail = onSendFail;
            abortTokenSource = new CancellationTokenSource();
            abortToken = abortTokenSource.Token;

            ws = new ClientWebSocket();

            connectTask = OpenSocketAsync(endpoint);
        }

        public void Send(int requestCounter, string requestId, string message) =>
            sendTask = sendTask.ContinueWith(async (task) => await SendAsync(requestCounter, requestId, message)).Unwrap();

        public void Update()
        {
        }

        public async Task CloseAsync()
        {
            var disposedWs = Interlocked.CompareExchange(ref ws, null,
                ws);

            if (disposedWs == null)
            {
                return;
            }

            await connectTask;
            CancelAndDisposeToken();
            await receiveTask;
            await sendTask;
            await CloseSocketAsync(disposedWs);

            try
            {
                disposedWs.Dispose();
            }
            catch (Exception exception)
            {
                logger.Debug("Disposing DotNet WebSocket failed",
                    ("exception", exception));
            }
        }

        private async Task OpenSocketAsync(string endpoint)
        {
            try
            {
                await ws.ConnectAsync(new Uri(endpoint), abortToken);
            }
            catch (WebSocketException webSocketException)
            {
                if (webSocketException.InnerException is OperationCanceledException)
                {
                    // This particular exception is expected if the
                    // connection is closed while connecting, so it's not an error.
                    return;
                }

                OnWebSocketFail?.Invoke(Error.RuntimeWebsocketCloudFailed, webSocketException.Message);
                return;
            }
            catch (Exception ex)
            {
                OnWebSocketFail?.Invoke(Error.RuntimeWebsocketOpenFailed, ex.Message);
                return;
            }

            receiveTask = RunReceive();
            OnConnect?.Invoke();
        }

        private async Task RunReceive()
        {
            var buffer = new byte[1 * 1024 * 1024];
            int receivedBytes = 0;

            try
            {
                while (!abortToken.IsCancellationRequested)
                {
                    try
                    {
                        if (ws == null)
                        {
                            return;
                        }

                        var receiveBuffer = new ArraySegment<byte>(buffer, receivedBytes, buffer.Length - receivedBytes);
                        var received = await ws.ReceiveAsync(receiveBuffer.AsMemory(), abortToken);
                        receivedBytes += received.Count;

                        if (received.MessageType == WebSocketMessageType.Close)
                        {
                            await CloseAsync();
                            return;
                        }

                        if (!received.EndOfMessage)
                        {
                            if (receivedBytes >= buffer.Length)
                            {
                                throw new WebSocketException($"Response message size over the limit of {buffer.Length} bytes");
                            }

                            continue;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        // Mono specific case - that's nullref from the ClientWebSocket internals
                        // which means that the connection was closed.
                        throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    if (!string.IsNullOrEmpty(message))
                    {
                        OnReceive?.Invoke(message);
                    }

                    receivedBytes = 0;
                }
            }
            catch (Exception exception)
            {
                switch (exception)
                {
                    case OperationCanceledException _:
                        break;
                    case WebSocketException { WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely }:
                        logger.Warning(Warning.RuntimeWebsocketClosedPrematurely,
                            ("exception", exception));
                        break;
                    default:
                        logger.Error(Error.RuntimeWebsocketReceiveException,
                            ("exception", exception));
                        OnWebSocketFail?.Invoke(Error.RuntimeWebsocketReceiveException, exception.Message);
                        break;
                }
            }
        }

        private async Task CloseSocketAsync(ClientWebSocket socket)
        {
            try
            {
                logger.Debug("Closing", ("Socket State", socket.State));

                var wasOpen = socket.State != WebSocketState.Open;

                try
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                catch { /* Ignore */ }

                if (wasOpen)
                {
                    OnDisconnect?.Invoke();
                }
            }
            catch (Exception exception)
            {
                logger.Debug("Closing failed", ("exception", exception));
            }
        }

        private void CancelAndDisposeToken()
        {
            if (abortTokenSource == null)
            {
                return;
            }

            abortTokenSource.Cancel();
            abortTokenSource.Dispose();
            abortTokenSource = null;
        }

        private async Task SendAsync(int requestCounter, string requestId, string text)
        {
            while (!IsConnected() && !abortToken.IsCancellationRequested)
            {
                await TimeSpan.FromMilliseconds(50);
            }

            if (abortToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var sendBuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));
                await ws.SendAsync(sendBuf, WebSocketMessageType.Text, true, abortToken);
            }
            catch (Exception exception)
            {
                if (abortToken.IsCancellationRequested)
                {
                    return;
                }

                switch (exception)
                {
                    case OperationCanceledException _:
                    case ObjectDisposedException _: // Cancellation token was disposed
                    case WebSocketException { WebSocketErrorCode: WebSocketError.InvalidState }:
                        break;
                    case NullReferenceException _:
                    case WebSocketException { WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely }:
                        logger.Warning(Warning.RuntimeWebsocketClosedPrematurely, ("requestID", requestId));
                        break;
                    default:
                        logger.Error(Error.RuntimeWebsocketSendException,
                            ("requestID", requestId),
                            ("exception", exception));
                        break;
                }

                OnSendFail?.Invoke(requestCounter, requestId, Error.RuntimeWebsocketSendException, exception.Message);
            }
        }
    }
}
