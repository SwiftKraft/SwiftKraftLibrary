namespace Coherence.Runtime
{
    internal static class WebSocketFactory
    {
        public static IWebSocket CreateWebSocket()
        {
#if UNITY_SWITCH && !UNITY_EDITOR && COHERENCE_HAS_NN_WEBSOCKET
            return new SwitchWebSocket();
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGLWebSocket();
#else
            return new DotNetWebSocket();
#endif
        }
    }
}
