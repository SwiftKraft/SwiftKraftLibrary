// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_WEBGL && !UNITY_EDITOR
namespace Coherence.Runtime
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Log;
    using Web;

    public sealed class WebGLWebSocket : IWebSocket
    {
        public bool PingWebSocket => true;

        private event Action OnConnect;
        private event Action OnDisconnect;
        private event Action<Error, string> OnWebSocketFail;
        private event Action<string> OnReceive;
        private event Action<int, string, Error, string> OnSendFail;

        private bool isWebGlConnected;
        private int id;

        private readonly Logger logger = Log.GetLogger<WebGLWebSocket>();

        public WebGLWebSocket()
        {
            id = GetHashCode();
        }

        public bool IsConnected() => isWebGlConnected;

        public void OpenSocket(string endpoint, Action onConnect, Action onDisconnect,
            Action<string> onReceive, Action<Error, string> onError, Action<int, string, Error, string> onSendFail)
        {
            logger.Debug("Connecting", ("Endpoint", endpoint));
            OnConnect = onConnect;
            OnDisconnect = onDisconnect;
            OnWebSocketFail = onError;
            OnReceive = onReceive;
            OnSendFail = onSendFail;

            WebSocketInterop.InitializeConnection(id, OnWebMessage, OnWebConnect, OnWebDisconnect, OnWebError);
            WebSocketInterop.ConnectSocket(id, endpoint);
        }

        public Task CloseAsync()
        {
            WebSocketInterop.DisconnectSocket(id);
            return Task.CompletedTask;
        }

        public void Send(int requestCounter, string requestId, string message) => WebSocketInterop.SendSocketMessage(id, message);

        public void Update()
        {
        }

        private void OnWebMessage(string message) => OnReceive?.Invoke(message);

        private void OnWebConnect()
        {
            isWebGlConnected = true;
            OnConnect?.Invoke();
        }

        private void OnWebDisconnect()
        {
            isWebGlConnected = false;
            OnDisconnect?.Invoke();
        }

        private void OnWebError(string error)
        {
            WebSocketInterop.DisconnectSocket(id);
            isWebGlConnected = false;
            OnWebSocketFail?.Invoke(Error.RuntimeWebsocketCloudFailed, error);
        }
    }
}
#endif
