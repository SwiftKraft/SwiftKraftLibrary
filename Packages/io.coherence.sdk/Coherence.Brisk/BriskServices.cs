// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk
{
    using Common;
    using Log;
    using System;
    using Transport;

    public class BriskServices
    {
        public static BriskServices Default { get; } = new BriskServices()
        {
            ConnectionTimerProvider = () => new SystemStopwatch(),
            SendTimerProvider = () => new SystemStopwatch(),
            KeepAliveProvider = UseBriskKeepAlive,
        };

        public Func<bool> KeepAliveProvider { get; set; }
        public Func<IStopwatch> SendTimerProvider { get; set; }
        public Func<IStopwatch> ConnectionTimerProvider { get; set; }
        internal Func<Logger, ITransport> TransportFactory { get; set; }

        private static bool UseBriskKeepAlive()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // KeepAlive is disabled only for WebGL which uses reliable
            // transport and has its own keep alive mechanisms.
            return false;
#else
            return true;
#endif
        }
    }
}
