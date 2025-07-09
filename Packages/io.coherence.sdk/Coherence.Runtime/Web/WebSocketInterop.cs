// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_WEBGL && !UNITY_EDITOR
namespace Coherence.Runtime.Web
{
    using AOT;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public struct WebSocketCallbacks
    {
        public Action disconnect;
        public Action connect;
        public Action<string> message;
        public Action<string> error;
    }

    public delegate void ConnectCallback(int id);
    public delegate void DisconnectCallback(int id);
    public delegate void ErrorCallback(int id, string msg);
    public delegate void MessageCallback(int id, string msg);

    public static class WebSocketInterop
    {
        private static Dictionary<int, WebSocketCallbacks> _callbacks = new Dictionary<int, WebSocketCallbacks>();

        public static void InitializeConnection(int id, Action<string> onMessage, Action onConnect, Action onDisconnect,
            Action<string> onError)
        {
            InitSocket(id, OnMessage, OnConnect, OnDisconnect, OnError);
            _callbacks[id] = new WebSocketCallbacks { disconnect = onDisconnect, connect = onConnect, error = onError, message = onMessage};
        }

        [DllImport("__Internal")]
        private static extern void InitSocket(int id, MessageCallback onMessage, ConnectCallback onConnect, DisconnectCallback onDisconnect, ErrorCallback onError);

        [DllImport("__Internal")]
        public static extern void ConnectSocket(int id, string connectionUri);

        [DllImport("__Internal")]
        public static extern void DisconnectSocket(int id);

        [DllImport("__Internal")]
        public static extern void SendSocketMessage(int id, string data);

        [MonoPInvokeCallback(typeof(ConnectCallback))]
        private static void OnConnect(int id)
        {
            _callbacks[id].connect();
        }

        [MonoPInvokeCallback(typeof(DisconnectCallback))]
        private static void OnDisconnect(int id)
        {
            _callbacks[id].disconnect();
        }

        [MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void OnError(int id, string msg)
        {
            _callbacks[id].error(msg);
        }

        [MonoPInvokeCallback(typeof(MessageCallback))]
        private static void OnMessage(int id, string msg)
        {
            _callbacks[id].message(msg);
        }
    }
}
#endif
