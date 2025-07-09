// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Transport.Web
{
#if UNITY
    using AOT;
#endif
    using Log;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public struct WebCallbacks
    {
        public Action OnOpen;
        public Action<byte[]> OnPacket;
        public Action<JsError> OnError;
    }

    public delegate void OnConnectCallback(int id);
    public delegate void OnPacketCallback(int id, int length, IntPtr ptr);
    public delegate void OnErrorCallback(int id, string errorJson);

    public static class WebInterop
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        public static extern void WebConnect(int id, string host, int roomId, string token, string uniqueRoomId, string worldId,
            string region, string schemaId);

        [DllImport("__Internal")]
        public static extern void WebDisconnect(int id);

        [DllImport("__Internal")]
        public static extern void WebSend(int id, byte[] data, int size);

        [DllImport("__Internal")]
        private static extern int WebInitialize(OnConnectCallback onConnect, OnPacketCallback onPacket, OnErrorCallback onError);
#else
        public static void WebConnect(int id, string host, int roomId, string token, string uniqueRoomId, string worldId,
            string region, string schemaId) { throw new InvalidOperationException("Non WebGL platform"); }
        public static void WebDisconnect(int id) { throw new InvalidOperationException("Non WebGL platform"); }
        public static void WebSend(int id, byte[] data, int size) { throw new InvalidOperationException("Non WebGL platform"); }
        private static int WebInitialize(OnConnectCallback onConnect, OnPacketCallback onPacket, OnErrorCallback onError) { throw new InvalidOperationException("Non WebGL platform"); }
#endif

        private static readonly Logger logger = Log.GetLogger(typeof(WebInterop));

        private static readonly Dictionary<int, WebCallbacks> callbacks = new Dictionary<int, WebCallbacks>();
        private static readonly JsonSerializerSettings jsErrorSerializationSettings = new JsonSerializerSettings()
        {
            Error = (sender, args) =>
            {
                logger.Warning(Warning.JSONSerialization,
                ("name", nameof(JsError)),
                ("Path", args.ErrorContext.Path),
                ("Exception", args.ErrorContext.Error.Message));

                args.ErrorContext.Handled = true;
            }
        };

        public static int InitializeConnection(Action onOpen, Action<byte[]> onPacket, Action<JsError> onError)
        {
            var id = WebInitialize(OnConnect, OnPacket, OnError);

            var cbs = new WebCallbacks
            {
                OnOpen = onOpen,
                OnPacket = onPacket,
                OnError = onError
            };

            callbacks.Add(id, cbs);

            return id;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(OnConnectCallback))]
#endif
        private static void OnConnect(int id)
        {
            try
            {
                if (callbacks.TryGetValue(id, out var cb))
                {
                    cb.OnOpen();
                }
            }
            catch (Exception ex)
            {
                logger.Error(Error.WebInteropOnOpen, ("exception", ex));
            }
        }

#if UNITY
        [MonoPInvokeCallback(typeof(OnPacketCallback))]
#endif
        private static void OnPacket(int id, int length, IntPtr ptr)
        {
            try
            {
                if (callbacks.TryGetValue(id, out var cb))
                {
                    byte[] data = new byte[length];
                    Marshal.Copy(ptr, data, 0, length);
                    cb.OnPacket(data);
                }
            }
            catch (Exception ex)
            {
                logger.Error(Error.WebInteropOnPacket, ("exception", ex));
            }
        }

#if UNITY
        [MonoPInvokeCallback(typeof(OnErrorCallback))]
#endif
        private static void OnError(int id, string errorJson)
        {
            try
            {
                if (callbacks.TryGetValue(id, out var cb))
                {
                    var error = Coherence.Utils.CoherenceJson.DeserializeObject<JsError>(errorJson, jsErrorSerializationSettings);
                    cb.OnError(error);
                }
            }
            catch (Exception ex)
            {
                logger.Error(Error.WebInteropOnError, ("exception", ex));
            }
        }
    }
}
