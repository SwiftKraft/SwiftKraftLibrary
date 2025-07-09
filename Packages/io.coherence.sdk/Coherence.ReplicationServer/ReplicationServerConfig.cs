// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.ReplicationServer
{
    using Coherence.Common;

    public delegate void LogHandler(string log);
    public delegate void ExitHandler(int code);

    public interface IReplicationServer
    {
        public event LogHandler OnLog;
        public event ExitHandler OnExit;

        public bool Start();
        public bool Stop(int timeoutMs = 0);
    }

    public struct ReplicationServerConfig
    {
#if COHERENCE_HAS_RSL
        public bool UseLite;
#endif
        public Mode Mode;
        public ushort APIPort;
        public ushort UDPPort;
        public ushort SignallingPort;
        public int SendFrequency;
        public int ReceiveFrequency;
        public bool DisableThrottling;
        public uint? DisconnectTimeout;
        public string Token;
        public LogTargetConfig[] LogTargets;
        public bool UseDebugStreams;
        public int AutoShutdownTimeout;
        public HostAuthority HostAuthority;
    }

    public enum Mode
    {
        World,
        Rooms
    }

    public struct LogTargetConfig
    {
        public LogTarget Target;
        public LogFormat Format;
        public Log.LogLevel LogLevel;
        public string FilePath;
    }

    public enum LogTarget
    {
        Console,
        File
    }

    public enum LogFormat
    {
        Plain,
        Colored,
        JSON
    }
}

namespace Coherence
{
#if COHERENCE_HAS_RSL
    using Coherence.Toolkit.ReplicationServer;

    public static class ReplicationServerOverride
    {
        public static System.Func<ReplicationServerConfig, IReplicationServer> LaunchRSLite = null;
    }
#endif
}
