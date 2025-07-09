// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Build;
    using UnityEditor;
    using Coherence.Toolkit;
    using System.IO;
    using Connection;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    internal static class SimulatorEditorUtility
    {
        private const string lastEndpointKey = "Coherence.Simulator.Endpoint";
        private const string buildPathSuffix = "SimulatorBuild";
        private const string lastLocalBuildPathKey = "Coherence.Simulator.BuildPath";

        private static EndpointData endpoint;
        private static string localSimulatorExecutablePath;

        private static string UserBuildPath => "Temp/coherence";
        internal static string FullBuildLocationPath => $"{UserBuildPath}/{buildPathSuffix}";
        internal static string ExecutablePath => $"{FullBuildLocationPath}/{SimulatorBuildOptions.BuildName}";

        internal static string LocalSimulatorExecutablePath
        {
            get => localSimulatorExecutablePath;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    localSimulatorExecutablePath = value;
                    EditorUserSettings.SetConfigValue(lastLocalBuildPathKey, localSimulatorExecutablePath);
                }
            }
        }

        internal static EndpointData LastUsedEndpoint
        {
            get
            {
                if (!endpoint.Validate().isValid)
                {
                    EndpointData.TryParse(EditorUserSettings.GetConfigValue(lastEndpointKey),
                        out endpoint);
                }
                endpoint.authToken = AuthToken.ForLocalDevelopment(ConnectionType.Client);
                return endpoint;
            }
        }

        static SimulatorEditorUtility()
        {
            localSimulatorExecutablePath = EditorUserSettings.GetConfigValue(lastLocalBuildPathKey);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        internal static void SaveEndPoint(EndpointData endpointData)
        {
            endpoint = endpointData;
            EditorUserSettings.SetConfigValue(lastEndpointKey, endpoint.ToString());
        }

        internal static bool IsBuildTargetSupported(bool headlessMode, ScriptingImplementation scriptingImplementation)
        {
            var linuxAssembly = System.AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.FullName.Contains("UnityEditor.LinuxStandalone.Extensions"));

            if (linuxAssembly != null)
            {
                var type = linuxAssembly.GetType("UnityEditor.LinuxStandalone.PlayersInstalled");

                if (type != null)
                {
                    string propertyName = string.Empty;

                    if (headlessMode && scriptingImplementation == ScriptingImplementation.Mono2x)
                    {
                        propertyName = "ServerMono";
                    }
                    else if (headlessMode && scriptingImplementation == ScriptingImplementation.IL2CPP)
                    {
                        propertyName = "ServerIl2cpp";
                    }
                    else if (!headlessMode && scriptingImplementation == ScriptingImplementation.Mono2x)
                    {
                        propertyName = "Mono";
                    }
                    else if (!headlessMode && scriptingImplementation == ScriptingImplementation.IL2CPP)
                    {
                        propertyName = "Il2Cpp";
                    }

                    var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);

                    if (property != null)
                    {
                        return (bool)property.GetValue(null, null);
                    }
                }
            }

            return false;
        }

        internal static bool BuildPathHasSimulatorBuild()
        {
            return File.Exists(localSimulatorExecutablePath);
        }

        internal static bool CanRunLocalSimulator()
        {
            return BuildPathHasSimulatorBuild() && LastUsedEndpoint.Validate().isValid;
        }

        internal static bool CanRunLocalSimulator(EndpointData endpoint)
        {
            return BuildPathHasSimulatorBuild() && ValidateIpAndPort(endpoint);
        }

        internal static bool ValidateIpAndPort(EndpointData endpoint)
        {
            return ValidateIp(endpoint.host) && endpoint.port != 0;
        }

        internal static bool ValidateIp(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return false;
            }

            string pattern = @"^([1-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(\.([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])){3}$";
            Regex check = new Regex(pattern);

            return check.IsMatch(ip, 0);
        }

        internal static void RunLocalSimulator()
        {
            RunLocalSimulator(LocalSimulatorExecutablePath, LastUsedEndpoint);
        }

        internal static void RunLocalSimulator(string buildPath, EndpointData endpoint)
        {
            string runCommand = GetLocalSimCommand(buildPath, endpoint);
            ProcessUtil.RunInTerminal(runCommand);
            EditorWindow.focusedWindow.ShowNotification(Icons.GetContentWithText("Coherence.Terminal", "Terminal loading."));
        }

        internal static string GetLocalSimCommand(string simPath, EndpointData endpoint)
        {
            string finalExePath = Path.GetFullPath(simPath);

            var command = $"\"{finalExePath}\" " +
                          "--coherence-simulation-server " +
                          $"--coherence-simulator-type {endpoint.simulatorType} " +
                          $"--coherence-region {endpoint.region} " +
                          $"--coherence-ip {endpoint.host} " +
                          $"--coherence-port {endpoint.port} " +
                          $"--coherence-world-id {endpoint.worldId} " +
                          $"--coherence-room-id {endpoint.roomId} " +
                          $"--coherence-unique-room-id {endpoint.uniqueRoomId}";

            if (endpoint.region != EndpointData.LocalRegion)
            {
                command += $" --coherence-auth-token {endpoint.authToken}";
            }

            return command;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (CoherenceBridgeStore.TryGetBridge(SceneManager.GetActiveScene(), out var bridge))
                    {
                        if (bridge.Client != null)
                        {
                            bridge.Client.OnConnectedEndpoint +=
                                (connectedEndpoint) =>
                                {
                                    endpoint = connectedEndpoint;
                                    EditorUserSettings.SetConfigValue(lastEndpointKey, endpoint.ToString());
                                };
                        }
                    }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }
    }
}
