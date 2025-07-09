// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Build
{
    using Editor;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using Transport;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.Callbacks;
    using UnityEditor.Compilation;
#if UNITY_EDITOR_OSX
    using UnityEditor.OSXStandalone;
#endif
    using UnityEngine;
    using Directory = UnityEngine.Windows.Directory;
    using File = UnityEngine.Windows.File;

    public static class SimulatorBuildPipeline
    {
        private static string DeferHeadlessBuildAfterCompilationKey => "io.coherence.deferheadlessbuild";
        private static string DeferLocalBuildAfterCompilationKey => "io.coherence.deferlocalbuild";
        private static string IsBuildingSimulatorKey => "io.coherence.isbuildingsim";
        private static string PreviousBuildTargetKey => "io.coherence.previousbuildtarget";
        private static string ProductNameKey => "io.coherence.productname";
        private static string ForceShowPromptKey => "io.coherence.forceshowprompt";
        private static string AppleSiliconWarningMessageKey => "io.coherence.applesiliconwarningmessage";
        private static string BuildTransportTypeKey => "io.coherence.buildtransporttype";
        internal static string PreviousLocalBuildPathKey => "io.coherence.previouslocalbuildpath";
        public static string ServerModuleMissingKey => "io.coherence.servermodulemissing";
        public static string UserConfirmedLocalServer = "io.coherence.userconfirmationoflocalserver";

        public static bool IsBuildingSimulator
        {
            get => SessionState.GetBool(IsBuildingSimulatorKey, false);
            internal set => SessionState.SetBool(IsBuildingSimulatorKey, value);
        }

        /// <summary>
        ///     This method must be called before BuildHeadlessLinuxClientAsync. The purpose of this method is to set the
        ///     required scripting symbol COHERENCE_SIMULATOR and the activeBuildSubTarget to Server, before building the Simulator itself.
        ///
        ///     It has to be done in two different method calls, because in Batch Mode, there is no Editor loop that ensures the code is recompiled
        ///     after setting the scripting symbols. See https://docs.unity3d.com/Manual/CustomScriptingSymbols.html
        /// </summary>
        public static void PrepareHeadlessBuild()
        {
            if (!IsInBatchMode())
            {
                return;
            }

            _ = ScriptingSymbolsChanger.ChangeScriptingSymbols(NamedBuildTarget.Server, false);

            var supportsHeadlessLinuxBuild =
                SimulatorEditorUtility.IsBuildTargetSupported(true,
                    SimulatorBuildOptions.Get().ScriptingImplementation);
            _ = ChangeBuildSubTarget(supportsHeadlessLinuxBuild);
            _ = ChangeBuildTargetToLinux();
        }

        /// <summary>
        ///     Method to build a Headless Linux Coherence Simulator, to be uploaded to the developer portal. It handles all the pre-configuration needed to build the Simulator successfully.
        ///
        ///     The asynchronous nature is due to having to set the scripting symbol COHERENCE_SIMULATOR and the activeBuildSubTarget to Server
        ///     before performing the build. This will trigger a full recompilation, unless the scripting symbol and the build sub target are already set.
        ///
        ///     The build will be performed after compilation finishes.
        /// </summary>
        public static void BuildHeadlessLinuxClientAsync()
        {
            ResetPrefs();

            DeleteFolder(SimulatorEditorUtility.FullBuildLocationPath);

            var options = SimulatorBuildOptions.Get();

            IsBuildingSimulator = true;

            var supportsHeadlessLinuxBuild =
                SimulatorEditorUtility.IsBuildTargetSupported(true, options.ScriptingImplementation);

            var changedScriptingSymbols = ScriptingSymbolsChanger.ChangeScriptingSymbols(
                supportsHeadlessLinuxBuild ? NamedBuildTarget.Server : NamedBuildTarget.Standalone, true);
            var changedBuildSubTarget = ChangeBuildSubTarget(supportsHeadlessLinuxBuild);
            var changedBuildTarget = ChangeBuildTargetToLinux();
            var changeRuntimeSettings = ChangeBuildToUdp();

            if (changedScriptingSymbols || changedBuildSubTarget || changedBuildTarget || changeRuntimeSettings)
            {
                SessionState.SetBool(DeferHeadlessBuildAfterCompilationKey, true);

                RequestCompilation();
            }
            else
            {
                PerformHeadlessBuild();
            }
        }

        private static bool ChangeBuildTargetToLinux()
        {
            bool changedBuildTarget = false;

            SessionState.SetInt(PreviousBuildTargetKey, (int)BuildTarget.NoTarget);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneLinux64)
            {
                changedBuildTarget = true;
                SessionState.SetInt(PreviousBuildTargetKey, (int)EditorUserBuildSettings.activeBuildTarget);
                _ = EditorUserBuildSettings.SwitchActiveBuildTarget(
                     BuildPipeline.GetBuildTargetGroup(BuildTarget.StandaloneLinux64), BuildTarget.StandaloneLinux64);
            }

            return changedBuildTarget;
        }

        private static bool ChangeBuildToUdp()
        {
            if (RuntimeSettings.Instance.TransportType != TransportType.UDPOnly)
            {
                SessionState.SetInt(BuildTransportTypeKey, (int)RuntimeSettings.Instance.TransportType);
                RuntimeSettings.Instance.TransportType = TransportType.UDPOnly;
                return true;
            }

            return false;
        }

        public static void RestoreTransportType()
        {
            RuntimeSettings.Instance.TransportType = (TransportType)SessionState.GetInt(BuildTransportTypeKey,
                (int)RuntimeSettings.Instance.TransportType);

            SessionState.EraseBool(DeferHeadlessBuildAfterCompilationKey);
        }

        /// <summary>
        ///     Method to build a Local Coherence Simulator, to be used locally. It handles all the pre-configuration needed to build the Simulator successfully.
        ///
        ///     The asynchronous nature is due to having to set the scripting symbol COHERENCE_SIMULATOR
        ///     before performing the build. This will trigger a full recompilation, unless the scripting symbol is already set.
        ///
        ///     The build will be performed after compilation finishes.
        /// </summary>
        /// <param name="forceShowPrompt">Force the dialog to appear, otherwise uses last-known good location from this session</param>
        public static void BuildLocalSimulator(bool forceShowPrompt = false)
        {
            ResetPrefs();
            SessionState.SetBool(ForceShowPromptKey, forceShowPrompt);
            var options = SimulatorBuildOptions.Get();

            IsBuildingSimulator = true;

            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            var namedBuildTarget = options.HeadlessMode ?
                NamedBuildTarget.Server : NamedBuildTarget.FromBuildTargetGroup(group);
            bool changedScriptingSymbols = ScriptingSymbolsChanger.ChangeScriptingSymbols(namedBuildTarget, false);
            bool changedBuildSubTarget = ChangeBuildSubTarget(options.HeadlessMode);

            if (changedScriptingSymbols || changedBuildSubTarget)
            {
                SessionState.SetBool(DeferLocalBuildAfterCompilationKey, true);
                RequestCompilation();
            }
            else
            {
                PerformLocalBuild();
            }
        }

        private static bool ChangeBuildSubTarget(bool useHeadlessMode)
        {
            if (EditorUserBuildSettings.standaloneBuildSubtarget != StandaloneBuildSubtarget.Server && useHeadlessMode)
            {
                EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
                return true;
            }

            if (EditorUserBuildSettings.standaloneBuildSubtarget != StandaloneBuildSubtarget.Player && !useHeadlessMode)
            {
                EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
                return true;
            }

            return false;
        }

        [DidReloadScripts(0)]
        internal static void PerformHeadlessBuildAfterCompilation()
        {
            var hasToBuild = SessionState.GetBool(DeferHeadlessBuildAfterCompilationKey, false);

            if (!hasToBuild)
            {
                return;
            }

            SessionState.SetBool(DeferHeadlessBuildAfterCompilationKey, false);

            EditorApplication.delayCall += PerformHeadlessBuild;
        }

        [DidReloadScripts(0)]
        internal static void PerformLocalBuildAfterCompilation()
        {
            var hasToBuild = SessionState.GetBool(DeferLocalBuildAfterCompilationKey, false);

            if (!hasToBuild)
            {
                return;
            }

            SessionState.SetBool(DeferLocalBuildAfterCompilationKey, false);

            EditorApplication.delayCall += PerformLocalBuild;
        }

        private static void RequestCompilation()
        {
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }

        private static void ResetPrefs()
        {
            SessionState.SetBool(DeferHeadlessBuildAfterCompilationKey, false);
            SessionState.SetBool(DeferLocalBuildAfterCompilationKey, false);
            SessionState.SetInt(PreviousBuildTargetKey, (int)BuildTarget.NoTarget);
        }

        private static void DeleteFolder(string buildLocationPath)
        {
            if (Directory.Exists(buildLocationPath))
            {
                Directory.Delete(buildLocationPath);
            }
        }

        private static bool IsInBatchMode()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError("This method is meant to be called in batch mode.");
                return false;
            }

            return true;
        }

        private static void PerformHeadlessBuild()
        {
            var options = SimulatorBuildOptions.Get();

            EditorBuildSettingsScene[] editorBuildSettingsScenes = GetEditorBuildSettingsScenes(options);

            if (editorBuildSettingsScenes.Length == 0)
            {
                Debug.LogError("No Scenes selected to be built in the Simulator");
                return;
            }

            BuildOptions buildOptions = GetBuildOptions(options.DevBuild);

            ChangeProductName();

            var report = BuildPipeline.BuildPlayer(editorBuildSettingsScenes, SimulatorEditorUtility.ExecutablePath, BuildTarget.StandaloneLinux64,
                buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                if (!Application.isBatchMode)
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (CompressAndUpload())
                        {
                            BuildSettingsRestorer.RestorePreviousBuildSettings(GetPreviousBuildTarget());
                        }
                    };
                }
                else
                {
                    _ = CompressAndUpload();
                }
            }
            else if (!Application.isBatchMode)
            {
                _ = EditorUtility.DisplayDialog("Simulator Build Failed",
                    "Simulator build has finished with errors. Your build settings and scripting symbols will not be reverted to preserve logs.",
                    "Ok");
            }

            IsBuildingSimulator = false;
            RestoreProductName();
        }

        private static BuildTarget GetPreviousBuildTarget()
        {
            var previousBuildTargetInt = SessionState.GetInt(PreviousBuildTargetKey, (int)BuildTarget.NoTarget);

            return (BuildTarget)previousBuildTargetInt;
        }

        private static void PerformLocalBuild()
        {
            var options = SimulatorBuildOptions.Get();

            var sceneNames = GetSceneNames(options);

            if (sceneNames.Length == 0)
            {
                Debug.LogError("No Scenes selected to be built in the Simulator");
                return;
            }

#if UNITY_EDITOR_OSX
            var currentArchitecture = UserBuildSettings.architecture;
#if UNITY_2022_1_OR_NEWER
            const OSArchitecture architectureToUse = OSArchitecture.x64;
#else
            const MacOSArchitecture architectureToUse = MacOSArchitecture.x64;
#endif
            var changeArchitecture = currentArchitecture != architectureToUse;

            if (changeArchitecture && (RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                RuntimeInformation.ProcessArchitecture == Architecture.Arm64))
            {
                if (Application.isBatchMode)
                {
                    Debug.Log("Arm builds can be detected as malware on certain versions of MacOS. Switching to Intel to build local simulator.");
                }

                if (Application.isBatchMode || EditorUtility.DisplayDialog("Architecture Has Been Changed",
                        "Local simulator will use Intel builds for computers running MacOS on Apple Silicon.", "Ok",
                        DialogOptOutDecisionType.ForThisSession, AppleSiliconWarningMessageKey))
                {
#if UNITY_2022_1_OR_NEWER
                    Debug.Log(
                        $"Architecture for simulator build has been changed from {currentArchitecture} to {OSArchitecture.x64}.");
                    UserBuildSettings.architecture = OSArchitecture.x64;
#else
                    Debug.Log(
                        $"Architecture for simulator build has been changed from {currentArchitecture} to {MacOSArchitecture.x64}.");
                    UserBuildSettings.architecture = MacOSArchitecture.x64;
#endif
                }
                else
                {
                    return;
                }
            }
#endif

            try
            {
                ChangeProductName();
                var forcePrompt = SessionState.GetBool(ForceShowPromptKey, false);
                var buildPath = SessionState.GetString(PreviousLocalBuildPathKey, string.Empty);
                if (forcePrompt || string.IsNullOrEmpty(buildPath))
                {
                    buildPath = GetBuildPathFromUser();
                }

                var buildOptions = GetBuildOptions(options.DevBuild);
                var report = BuildPipeline.BuildPlayer(sceneNames, buildPath,
                    EditorUserBuildSettings.activeBuildTarget, buildOptions);
                if (report.summary.result == BuildResult.Succeeded)
                {
                    SessionState.SetString(PreviousLocalBuildPathKey, buildPath);
                    SimulatorEditorUtility.LocalSimulatorExecutablePath = GetLocalSimulatorExecutablePath(buildPath);
                    Debug.Log($"Local Simulator Build succeeded! {buildPath}");
                }
                else
                {
#if UNITY_2023_1_OR_NEWER
                    var errors = report.SummarizeErrors();
#else
                    var errors = report.steps.Select(s => s.messages)
                        .SelectMany(m => m)
                        .Where(m => m.type == LogType.Error)
                        .Select(m => m.content)
                        .Aggregate("", (current, message) => $"{current} {message} ");
#endif
                    var regex = new Regex("Dedicated Server support for .* is not installed");
                    if (regex.IsMatch(errors))
                    {
                        Debug.LogError($"Local Simulator Build failed with {report.summary.totalErrors} errors.");
                        SessionState.SetBool(ServerModuleMissingKey, true);
                    }
                    Debug.LogError($"Local Simulator Build failed with {report.summary.totalErrors} errors.");
                }
            }
            catch (BuildPlayerWindow.BuildMethodException buildMethodException)
            {
                if (!string.IsNullOrEmpty(buildMethodException.Message))
                {
                    Debug.LogError($"Message: {buildMethodException}. StackTrace: {buildMethodException.StackTrace}");
                }
            }
            finally
            {
#if UNITY_EDITOR_OSX
                UserBuildSettings.architecture = currentArchitecture;
#endif
                IsBuildingSimulator = false;
                RestoreProductName();
                EditorApplication.delayCall += BuildSettingsRestorer.RestorePreviousBuildSettingsForLocalBuild;
            }
        }

        private static string GetBuildPathFromUser()
        {
            var isWindows =
                EditorUserBuildSettings.activeBuildTarget is BuildTarget.StandaloneWindows
                    or BuildTarget.StandaloneWindows64;

            var isLinux =
                EditorUserBuildSettings.activeBuildTarget is BuildTarget.LinuxHeadlessSimulation
                    or BuildTarget.StandaloneLinux64;

            var buildPath = isWindows || isLinux
                ? EditorUtility.SaveFilePanel("Build Dedicated Server", "", "", "")
                : EditorUtility.SaveFolderPanel("Build Dedicated Server", "", "");

            if (isWindows && !buildPath.EndsWith(SimulatorBuildOptions.BuildName + ".exe"))
            {
                var pathWithoutExtension = Path.Combine(Path.GetDirectoryName(buildPath),
                    Path.GetFileNameWithoutExtension(buildPath));
                if (!Directory.Exists(pathWithoutExtension))
                {
                    Directory.CreateDirectory(pathWithoutExtension);
                }

                buildPath = Path.Combine(pathWithoutExtension, SimulatorBuildOptions.BuildName + ".exe");
            }
            else if (isLinux && !buildPath.EndsWith(SimulatorBuildOptions.BuildName))
            {
                if (!Directory.Exists(buildPath))
                {
                    Directory.CreateDirectory(buildPath);
                }

                buildPath = Path.Combine(buildPath, SimulatorBuildOptions.BuildName);
            }

            return buildPath;
        }

        private static string GetLocalSimulatorExecutablePath(string filePath)
        {
            var isWindows =
                EditorUserBuildSettings.activeBuildTarget is BuildTarget.StandaloneWindows
                    or BuildTarget.StandaloneWindows64;
            var isMacOS = EditorUserBuildSettings.activeBuildTarget is BuildTarget.StandaloneOSX;

            if (!Path.HasExtension(filePath) || !isWindows)
            {
                return Path.Combine(filePath, isMacOS ? SimulatorBuildOptions.BuildName : "").Replace(
                    Path.DirectorySeparatorChar, '/');
            }

            var oldNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            if (string.Equals(oldNameWithoutExtension, SimulatorBuildOptions.BuildName))
            {
                return filePath;
            }

            var directory = Path.GetDirectoryName(filePath);
            var extension = Path.GetExtension(filePath);
            return (Path.Combine(directory, SimulatorBuildOptions.BuildName) + extension).Replace(
                Path.DirectorySeparatorChar, '/');
        }

        private static BuildPlayerOptions GetBuildPlayerOptions(bool devBuild, string[] scenes)
        {
            var defaultOptions = new BuildPlayerOptions();
            defaultOptions = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(defaultOptions);

            if (devBuild)
            {
                defaultOptions.options |= BuildOptions.Development;
            }
            defaultOptions.scenes = scenes;
            return defaultOptions;
        }

        private static BuildOptions GetBuildOptions(bool devBuild)
        {
            BuildOptions buildOptions = BuildOptions.None;

            if (devBuild)
            {
                buildOptions |= BuildOptions.Development;
            }

            return buildOptions;
        }

        private static EditorBuildSettingsScene[] GetEditorBuildSettingsScenes(SimulatorBuildOptions options)
        {
            var editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
            foreach (var sceneAsset in options.ScenesToBuild)
            {
                string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }

            return editorBuildSettingsScenes.ToArray();
        }

        private static string[] GetSceneNames(SimulatorBuildOptions options)
        {
            var sceneNames = new List<string>();

            foreach (var scene in options.ScenesToBuild)
            {
                var scenePath = AssetDatabase.GetAssetPath(scene);
                sceneNames.Add(scenePath);
            }

            return sceneNames.ToArray();
        }

        private static bool CompressAndUpload()
        {
            var simSlug = GetCmdLineArg("-simSlug");

            simSlug = string.IsNullOrEmpty(simSlug) ? RuntimeSettings.Instance.SimulatorSlug : simSlug;

            if (string.IsNullOrEmpty(simSlug))
            {
                _ = EditorUtility.DisplayDialog("Simulator", "Simulator slug is required", "OK");
                return false;
            }

            ProjectSettings.instance.RuntimeSettings.SimulatorSlug = simSlug;
            EditorUtility.SetDirty(ProjectSettings.instance.RuntimeSettings);

            string path = Path.Combine(Application.temporaryCachePath, Paths.simulatorZipFile);

            Analytics.Capture(Analytics.Events.UploadSimStart);

            try
            {
                EditorUtility.DisplayProgressBar("Simulator", "Compressing simulator build path...", 1f);
                File.Delete(path);
                ZipUtils.Zip(Path.GetFullPath(SimulatorEditorUtility.FullBuildLocationPath), path);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            try
            {
                long size = new FileInfo(path).Length;

                // request a valid upload endpoint
                var uurl = Coherence.Editor.Portal.UploadURL.GetSimulator(size);
                if (uurl == null)
                {
                    return false;
                }

                // upload the simulator (zipfile)
                if (!uurl.Upload(path, size))
                {
                    return false;
                }

                // instruct the portal to deploy the uploaded simulator
                _ = uurl.RegisterSimulator();

                Analytics.Capture(Analytics.Events.UploadSimEnd);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                File.Delete(path);
            }

            return true;
        }

        private static string GetCmdLineArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }
            return null;
        }


        private static void ChangeProductName()
        {
            EditorPrefs.SetString(ProductNameKey, PlayerSettings.productName);
            PlayerSettings.productName = SimulatorBuildOptions.BuildName;
        }

        private static void RestoreProductName()
        {
            var productName = EditorPrefs.GetString(ProductNameKey);
            PlayerSettings.productName = productName;
        }
    }
}
