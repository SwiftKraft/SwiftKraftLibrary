// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.ReplicationServer
{
    using System;
    using System.IO;
    using System.Text;
    using Coherence.Common;
    using Plugins.NativeLauncher;
    using UnityEngine;

    public static class Launcher
    {
        internal static IConfigProvider ConfigProvider = new StandaloneLauncherConfig();

        internal static IReplicationServer Start(ReplicationServerConfig serverConfig, bool startInTerminal,
            string additionalArguments = null)
        {
            var executablePath = ConfigProvider.ExecutablePath;
            var arguments = GenerateArguments(serverConfig, additionalArguments);

            ProcessUtil.FixUnixPermissions(executablePath);

            var command = ProcessUtil.CommandFromExecutableAndArguments(executablePath, arguments);
            var process = startInTerminal
                ? ProcessUtil.RunInTerminal(command)
                : ProcessUtil.RunOutsideTerminal(executablePath, arguments);

            return new ReplicationServer(process);
        }

        /// <summary>
        /// Create a new `ReplicationServer` instance with the given configuration.
        /// Has to be started using `Start`, and allows `OnLog` and `OnExit` events to be subscribed to.
        /// </summary>
        /// <param name="serverConfig">Configuration to use when starting the server.</param>
        /// <param name="additionalArguments">Optional flags to add when starting the server.</param>
        /// <returns>A new `ReplicationServer`, to be started with `Start`.</returns>
        public static IReplicationServer Create(ReplicationServerConfig serverConfig, string additionalArguments = null)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.PS4:
                case RuntimePlatform.PS5:
                case RuntimePlatform.Switch:
                case RuntimePlatform.XboxOne:
                case RuntimePlatform.GameCoreXboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
#if !COHERENCE_HAS_RSL
                    throw new InvalidOperationException("Console builds require special integration for client hosting. Please contact the coherence team for details.");
#else
                    serverConfig.UseLite = true;
                    break;
#endif
            }
#if COHERENCE_HAS_RSL
            if (serverConfig.UseLite)
            {
                if (ReplicationServerOverride.LaunchRSLite != null)
                {
                    return ReplicationServerOverride.LaunchRSLite.Invoke(serverConfig);
                }

                throw new InvalidOperationException("Replication Server Lite not ready yet.");
            }
#endif

            var executablePath = ConfigProvider.ExecutablePath;
            var arguments = GenerateArguments(serverConfig, additionalArguments);

            var startupInfo = new NlProcessStartupInfo(executablePath, arguments)
            {
                RaiseOnExit = true,
            };
            startupInfo.EnvironmentVariables.Add("RS_UNITY_VERSION", Application.unityVersion);

            return new ReplicationServer(new NlProcess(startupInfo));
        }

        private static string GenerateArguments(ReplicationServerConfig serverConfig, string additionalArguments = null)
        {
            additionalArguments ??= "";

            var typeArgument = serverConfig.Mode switch
            {
                Mode.World => "world",
                Mode.Rooms => "rooms",
                _ => throw new ArgumentException("Unsupported mode", nameof(serverConfig.Mode)),
            };

            var tokenArgument = !string.IsNullOrEmpty(serverConfig.Token)
                ? $" --unlock-token {serverConfig.Token}"
                : "";

            var timeoutArgument = serverConfig.DisconnectTimeout.HasValue
                ? $" --disconnect-timeout {serverConfig.DisconnectTimeout.Value}"
                : "";

            var debugStreamsArgument = (serverConfig.Mode == Mode.World && serverConfig.UseDebugStreams)
                ? " --debug-streams"
                : "";

            var disableThrottlingArgument = (serverConfig.DisableThrottling)
                ? " --disable-throttling"
                : "";

            var logTargetsArguments = GenerateLogTargetArguments(serverConfig.LogTargets);

            var schemas = string.Join(",", ConfigProvider.GatherSchemaPaths());

            var arguments = typeArgument +
                            $" --api-port {serverConfig.APIPort}" +
                            $" --udp-port {serverConfig.UDPPort}" +
                            $" --signalling-port {serverConfig.SignallingPort}" +
                            $" --send-frequency {serverConfig.SendFrequency}" +
                            $" --recv-frequency {serverConfig.ReceiveFrequency}" +
                            (serverConfig.Mode == Mode.World
                                ? $" --schema \"{schemas}\""
                                : $" --default-schema \"{schemas}\"") +
                            debugStreamsArgument +
                            disableThrottlingArgument +
                            tokenArgument +
                            timeoutArgument +
                            logTargetsArguments +
                            $" {additionalArguments}";

            if (serverConfig.AutoShutdownTimeout != 0)
            {
                arguments += $" --auto-shutdown-timeout-ms {serverConfig.AutoShutdownTimeout}";
            }

            if (serverConfig.HostAuthority != 0)
            {
                arguments += $" --host-authority-features {serverConfig.HostAuthority.GetCommaSeparatedFeatures()}";
            }

            return arguments;
        }

        internal static string GenerateLogTargetArguments(LogTargetConfig[] logTargets)
        {
            if (logTargets == null || logTargets.Length == 0)
            {
                return "";
            }

            var stringBuilder = new StringBuilder();

            foreach (var logTarget in logTargets)
            {
                stringBuilder.Append(" --log-target ");
                stringBuilder.Append(logTarget.Target.ToString().ToLowerInvariant());
                stringBuilder.Append(":");
                stringBuilder.Append(logTarget.Format.ToString().ToLowerInvariant());
                stringBuilder.Append(":");
                stringBuilder.Append(logTarget.LogLevel.ToString().ToLowerInvariant());
                if (logTarget.Target == LogTarget.File)
                {
                    if (string.IsNullOrEmpty(logTarget.FilePath))
                    {
                        throw new Exception("Missing FilePath for file log target");
                    }

                    stringBuilder.Append(":");
                    stringBuilder.Append('"');
                    stringBuilder.Append(logTarget.FilePath);
                    stringBuilder.Append('"');
                }
            }

            return stringBuilder.ToString();
        }

        internal static string ToCommand(ReplicationServerConfig serverConfig, string additionalArguments = null)
        {
            var executablePath = ConfigProvider.ExecutablePath;
            var arguments = GenerateArguments(serverConfig, additionalArguments);

            return $"\"{executablePath}\" {arguments}";
        }
    }

    public class StandaloneLauncherConfig : IConfigProvider
    {
        public string ExecutablePath
        {
            get
            {
#if UNITY_STANDALONE_WIN
                return Path.GetFullPath($"{Application.streamingAssetsPath}/replication-server.exe");
#else
                return Path.GetFullPath($"{Application.streamingAssetsPath}/replication-server");
#endif
            }
        }

        public string[] GatherSchemaPaths() =>
            new[]
            {
                Path.GetFullPath($"{Application.streamingAssetsPath}/combined.schema"),
            };
    }
}
