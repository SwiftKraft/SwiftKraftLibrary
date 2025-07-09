// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.ReplicationServer
{
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using Coherence.Toolkit.ReplicationServer;
    using UnityEditor;

    [InitializeOnLoad]
    public static class EditorLauncher
    {
        public static string AdditionalServerArgs => "--env dev";

        private const string StartInTerminalKey = "Coherence.startInTerminal";

        public static bool StartInTerminal
        {
            get => UserSettings.GetBool(StartInTerminalKey, true);
            set => UserSettings.SetBool(StartInTerminalKey, value);
        }

        static EditorLauncher()
        {
            Launcher.ConfigProvider = new EditorConfigProvider();
        }

        public static ReplicationServerConfig CreateLocalWorldConfig()
        {
            var projectSettings = ProjectSettings.instance;
            var infiniteTimeout = projectSettings.keepConnectionAlive;

            var config = new ReplicationServerConfig
            {
                Mode = Mode.World,
                APIPort = (ushort)projectSettings.RuntimeSettings.WorldsAPIPort,
                UDPPort = (ushort)projectSettings.worldUDPPort,
                SignallingPort = (ushort)projectSettings.worldWebPort,
                SendFrequency = projectSettings.sendFrequency,
                ReceiveFrequency = projectSettings.recvFrequency,
                DisconnectTimeout = infiniteTimeout ? uint.MaxValue : null,
                DisableThrottling = false,
                Token = RuntimeSettings.Instance.ReplicationServerToken,
                LogTargets = GetLogTargets(projectSettings),
                UseDebugStreams = RuntimeSettings.Instance.UseDebugStreams,
                HostAuthority = projectSettings.GetHostAuthority(),
            };

            return config;
        }

        public static ReplicationServerConfig CreateLocalRoomsConfig()
        {
            var projectSettings = ProjectSettings.instance;
            var infiniteTimeout = projectSettings.keepConnectionAlive;

            var config = new ReplicationServerConfig
            {
                Mode = Mode.Rooms,
                APIPort = (ushort)projectSettings.RuntimeSettings.APIPort,
                UDPPort = (ushort)projectSettings.roomsUDPPort,
                SignallingPort = (ushort)projectSettings.roomsWebPort,
                SendFrequency = projectSettings.sendFrequency,
                ReceiveFrequency = projectSettings.recvFrequency,
                DisableThrottling = false,
                DisconnectTimeout = infiniteTimeout ? uint.MaxValue : null,
                Token = RuntimeSettings.Instance.ReplicationServerToken,
                LogTargets = GetLogTargets(projectSettings),
                HostAuthority = 0, // This is set in the create room API
            };

            return config;
        }

        public static void RunWorldsReplicationServerInTerminal()
        {
            Analytics.Capture(Analytics.Events.RunLocalReplicatorWorlds);

            var config = CreateLocalWorldConfig();
            Launcher.Start(config, StartInTerminal, AdditionalServerArgs);
        }

        public static void RunRoomsReplicationServerInTerminal()
        {
            Analytics.Capture(Analytics.Events.RunLocalReplicatorRooms);

            var config = CreateLocalRoomsConfig();
            Launcher.Start(config, StartInTerminal, AdditionalServerArgs);
        }

        private static LogTargetConfig[] GetLogTargets(ProjectSettings projectSettings)
        {
            var consoleTarget = new LogTargetConfig
            {
                Target = LogTarget.Console,
                Format = LogFormat.Colored,
                LogLevel = projectSettings.rsConsoleLogLevel,
            };

            var fileTarget = new LogTargetConfig
            {
                Target = LogTarget.File,
                Format = LogFormat.JSON,
                LogLevel = projectSettings.rsFileLogLevel,
                FilePath = projectSettings.rsLogFilePath,
            };

            if (projectSettings.rsLogToFile)
            {
                return new[]
                {
                    consoleTarget,
                    fileTarget,
                };
            }

            return new[]
            {
                consoleTarget,
            };
        }

        private class EditorConfigProvider : IConfigProvider
        {
            public string ExecutablePath => Path.GetFullPath(Paths.ReplicationServerPath);

            public string[] GatherSchemaPaths()
            {
                // initial capacity = toolkit + gather [+ combined] covers most if not all known scenarios
                var schemas = new List<string>(3);

                var toolkit = Path.GetFullPath(Paths.toolkitSchemaPath);
                schemas.Add(toolkit);

                var gathered = Path.GetFullPath(Paths.gatherSchemaPath);
                schemas.Add(gathered);

                var schemaAssets = ProjectSettings.instance.activeSchemas;
                foreach (var asset in schemaAssets)
                {
                    var schemaPath = Path.GetFullPath(AssetDatabase.GetAssetPath(asset));
                    schemas.Add(schemaPath);
                }

                if (ProjectSettings.instance.RuntimeSettings.extraSchemas != null)
                {
                    var schemaPaths = ProjectSettings.instance.RuntimeSettings.extraSchemas
                        .Where(s => s != null)
                        .Select(s => Path.GetFullPath(AssetDatabase.GetAssetPath(s)));
                    schemas.AddRange(schemaPaths);
                }

                return schemas.ToArray();
            }
        }
    }
}
