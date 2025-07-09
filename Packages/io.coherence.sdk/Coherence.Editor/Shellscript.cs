// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using Coherence.Toolkit.ReplicationServer;
    using ReplicationServer;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;

    public static class Shellscript
    {
#if UNITY_EDITOR_LINUX
        const string format = @"#!/bin/bash
 if [ ""$#"" -eq  ""0"" ]; then
exec {0}
else
exec {0} ""$@""
fi
";
#elif UNITY_EDITOR_OSX
        const string format = "#!/bin/sh\n{0}\n";
#else
        const string format = "{0}\n";
#endif

        [DidReloadScripts(-500)]
        public static void ExportTerminalCommands()
        {
            if (EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ExportTerminalCommands;
                return;
            }

            var roomsCommand = Launcher.ToCommand(EditorLauncher.CreateLocalRoomsConfig());
            var worldCommand = Launcher.ToCommand(EditorLauncher.CreateLocalWorldConfig());

            WriteTerminalRoomsCommand(roomsCommand);
            WriteTerminalWorldCommand(worldCommand);
        }

        public static void WriteTerminalRoomsCommand(string roomsCommand)
        {
#if UNITY_EDITOR_WIN
            WriteFileToLibrary("run-replication-server-rooms.bat", string.Format(format, roomsCommand));
#else
            WriteFileToLibrary("run-replication-server-rooms.sh", string.Format(format, roomsCommand));
#endif
        }

        public static void WriteTerminalWorldCommand(string worldCommand)
        {
#if UNITY_EDITOR_WIN
            WriteFileToLibrary("run-replication-server-worlds.bat", string.Format(format, worldCommand));
#else
            WriteFileToLibrary("run-replication-server-worlds.sh", string.Format(format, worldCommand));
#endif
        }

        internal static string ExportSimulatorBuildCommand()
        {
            var stringBuilder = new StringBuilder();
            string fileName = string.Empty;
#if UNITY_EDITOR_WIN
            fileName = "build_headless_linux_client.bat";
            stringBuilder.AppendLine("@echo off");
            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine(
                $"\"{EditorApplication.applicationPath}\" -projectPath \"{Path.GetDirectoryName(Application.dataPath)}\" -batchmode -nographics -quit -executeMethod Coherence.Build.SimulatorBuildPipeline.PrepareHeadlessBuild -logFile \"{Path.GetFullPath(Paths.libraryRootPath + "/prepareBuildLog.txt")}\"");
            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine(
                $"\"{EditorApplication.applicationPath}\" -projectPath \"{Path.GetDirectoryName(Application.dataPath)}\" -batchmode -nographics -quit -executeMethod Coherence.Build.SimulatorBuildPipeline.BuildHeadlessLinuxClientAsync -simSlug {RuntimeSettings.Instance.SimulatorSlug} -logFile \"{Path.GetFullPath(Paths.libraryRootPath + "/headlessBuildLog.txt")}\"");
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            fileName = "build_headless_linux_client.sh";
            stringBuilder.AppendLine("#!/bin/sh");
            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine(
                $"{EditorApplication.applicationContentsPath}/MacOS/Unity -projectPath \"{Path.GetDirectoryName(Application.dataPath)}\" -batchmode -nographics -quit -executeMethod Coherence.Build.SimulatorBuildPipeline.PrepareHeadlessBuild -logFile \"{Path.GetFullPath(Paths.libraryRootPath + "/prepareBuildLog.txt")}\"");
            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine(
                $"{EditorApplication.applicationContentsPath}/MacOS/Unity -projectPath \"{Path.GetDirectoryName(Application.dataPath)}\" -batchmode -nographics -quit -executeMethod Coherence.Build.SimulatorBuildPipeline.BuildHeadlessLinuxClientAsync -simSlug {RuntimeSettings.Instance.SimulatorSlug} -logFile \"{Path.GetFullPath(Paths.libraryRootPath + "/headlessBuildLog.txt")}\"");
#endif

            return WriteFileToLibrary(fileName, stringBuilder.ToString());
        }

        private static string WriteFileToLibrary(string name, string contents)
        {
            string path = string.Empty;

            try
            {
                if (!Directory.Exists(Paths.libraryRootPath))
                {
                    _ = Directory.CreateDirectory(Paths.libraryRootPath);
                }

                path = $"{Paths.libraryRootPath}/{name}";

                File.WriteAllText(path, contents, Encoding.ASCII);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return path;
        }
    }
}
