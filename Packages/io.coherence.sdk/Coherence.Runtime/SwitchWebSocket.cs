// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_SWITCH && !UNITY_EDITOR && COHERENCE_HAS_NN_WEBSOCKET
namespace Coherence.Runtime
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Coherence.Log;

    public class SwitchWebSocket : IWebSocket
    {
        public bool PingWebSocket => true;
        
        private event Action OnConnect;
        private event Action OnDisconnect;
        private event Action<Error, string> OnWebSocketFail;
        private event Action<string> OnReceive;
        private event Action<int, string, Error, string> OnSendFail;
        
        private int id;
        private bool isWsConnected;

        private readonly Logger logger = Log.GetLogger<SwitchWebSocket>();

        public SwitchWebSocket()
        {
            id = GetHashCode();
        }

        public bool IsConnected() => isWsConnected;

        public void OpenSocket(string endpoint, Action onConnect, Action onDisconnect,
            Action<string> onReceive, Action<Error, string> onError, Action<int, string, Error, string> onSendFail)
        {
            logger.Debug("Connecting", ("Endpoint", endpoint));
            OnConnect = onConnect;
            OnDisconnect = onDisconnect;
            OnWebSocketFail = onError;
            OnReceive = onReceive;
            OnSendFail = onSendFail;
                
            SwitchWebSocketInterop.InitializeConnection(id, OnWsConnect, OnWsDisconnect, OnWsError, OnWsMessage);
            SwitchWebSocketInterop.coherence_switch_ConnectSocket(id, endpoint);
        }

        public void CloseSocket() => SwitchWebSocketInterop.coherence_switch_DisconnectSocket(id);

        public void Send(int requestCounter, string requestId, string message) =>
            SwitchWebSocketInterop.coherence_switch_Send(id, message, message.Length);
        
        public void Update() => SwitchWebSocketInterop.coherence_switch_Update(id);

        public Task CloseAsync()
        {
            SwitchWebSocketInterop.coherence_switch_DisconnectSocket(id);
            return Task.CompletedTask;
        }
        
        private void OnWsMessage(string message) => OnReceive?.Invoke(message);

        private void OnWsConnect()
        {
            isWsConnected = true;
            OnConnect?.Invoke();
        }

        private void OnWsDisconnect()
        {
            isWsConnected = false;
            OnDisconnect?.Invoke();
        }

        private void OnWsError(string error)
        {
            SwitchWebSocketInterop.coherence_switch_DisconnectSocket(id);
            isWsConnected = false;
            OnWebSocketFail?.Invoke(Error.RuntimeWebsocketCloudFailed, error);
        }
    }
}
#endif
