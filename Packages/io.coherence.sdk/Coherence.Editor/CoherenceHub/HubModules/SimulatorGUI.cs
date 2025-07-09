// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Build;
    using Connection;
    using Portal;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    internal static class SimulatorGUI
    {
        private static SerializedObject buildOptionsSo;
        private static string selectedRegion = EndpointData.LocalRegion;
        private static float minWindowWidth = 285f;
        private const string AtLeastOneSceneWarning = "Simulator build options need at least one scene";

        internal static void DrawSimulatorBuildOptions(SimulatorBuildOptions buildOptions, float windowWidth)
        {
            if (buildOptionsSo == null)
            {
                buildOptionsSo = new SerializedObject(buildOptions);
            }

            var headlessMode = buildOptionsSo.FindProperty("headlessMode");
            var scriptingImplementation = buildOptionsSo.FindProperty("scriptingImplementation");
            var developmentBuild = buildOptionsSo.FindProperty("developmentBuild");
            var buildSizeOptimizations = buildOptionsSo.FindProperty("buildSizeOptimizations");
            var replaceTextures = buildSizeOptimizations.FindPropertyRelative("stripAssets");
            var keepOriginalAssetsBackup = buildSizeOptimizations.FindPropertyRelative("backupAssets");
            var compressMeshes = buildSizeOptimizations.FindPropertyRelative("compressMeshes");
            var disableStaticBatching = buildSizeOptimizations.FindPropertyRelative("disableStaticBatching");

            var headlessModeContent = new GUIContent(headlessMode.displayName,
                "This option will always be true for Linux clients that are built to be uploaded to the Portal");
            var scriptingImplementationContent = new GUIContent(scriptingImplementation.displayName);
            var developmentBuildContent = new GUIContent(developmentBuild.displayName);
            var buildSizeOptimizationsContent = new GUIContent(buildSizeOptimizations.displayName);
            var replaceTexturesContent = new GUIContent(replaceTextures.displayName);
            var keepOriginalAssetsBackupContent = new GUIContent(keepOriginalAssetsBackup.displayName);
            var compressMeshessContent = new GUIContent(compressMeshes.displayName);
            var disableStaticBatchingContent = new GUIContent(disableStaticBatching.displayName);

            var maxWidth = CoherenceHubLayout.Styles.Label.CalcSize(headlessModeContent).x + 20f;
            // Extremely ugly hack to make the build options scale and wrap properly depending on window width
            maxWidth += windowWidth > minWindowWidth ? windowWidth - minWindowWidth : 0f;

            EditorGUILayout.PropertyField(buildOptionsSo.FindProperty("scenesToBuild"));

            DrawToggle(headlessModeContent, headlessMode, maxWidth);
            DrawGenericProperty(scriptingImplementationContent, scriptingImplementation, maxWidth);
            DrawToggle(developmentBuildContent, developmentBuild, maxWidth);

            EditorGUILayout.Separator();

            CoherenceHubLayout.DrawBoldLabel(buildSizeOptimizationsContent);
            DrawToggle(replaceTexturesContent, replaceTextures, maxWidth);
            DrawToggle(keepOriginalAssetsBackupContent, keepOriginalAssetsBackup, maxWidth);
            DrawToggle(compressMeshessContent, compressMeshes, maxWidth);
            DrawToggle(disableStaticBatchingContent, disableStaticBatching, maxWidth);

            buildOptionsSo.ApplyModifiedProperties();
        }

        private static void DrawGenericProperty(GUIContent content, SerializedProperty prop, float maxWidth)
        {
            using var scope = new EditorGUILayout.HorizontalScope(GUILayout.Width(maxWidth + 90f));
            CoherenceHubLayout.DrawLabel(content, GUILayout.Width(maxWidth));
            EditorGUILayout.PropertyField(prop, new GUIContent(string.Empty));
        }

        private static void DrawToggle(GUIContent content, SerializedProperty prop, float maxWidth)
        {
            using var scope = new EditorGUILayout.HorizontalScope(GUILayout.Width(maxWidth));
            CoherenceHubLayout.DrawLabel(content, GUILayout.Width(maxWidth));
            prop.boolValue = CoherenceHubLayout.DrawToggle(string.Empty, prop.boolValue, GUILayout.Width(40f));
        }

        internal static void DrawCreateAndUploadHeadlessSimulatorBuild(SimulatorBuildOptions buildOptions)
        {
            CoherenceHubLayout.DrawInfoLabel(new GUIContent("The build will be uploaded to your selected organization and project in the coherence Cloud."));

            if (HasOrgAndProject() && PortalUtil.CanCommunicateWithPortal)
            {
                DrawOrgAndProject();
            }

            EditorGUILayout.Space();

            DrawSimSlug();

            var simBuildBlockers = GetSimBuildBlockers(buildOptions);
            DrawCreateAndUploadBuildButton(simBuildBlockers.HasFlag(SimulatorBuildBlocker.CannotUploadLinuxBuild));
            DrawCreateAndUploadErrorMessages(simBuildBlockers, buildOptions.ScriptingImplementation);
            DrawCreateAndUploadInformation(simBuildBlockers);
        }

        internal static void DrawLocalSimulatorBuild(SimulatorBuildOptions buildOptions)
        {
            var symbolMessage = IsLocalBuildSymbolDefined(buildOptions)
                ? "The COHERENCE_SIMULATOR symbol has been set."
                : "The COHERENCE_SIMULATOR symbol will be added temporarily and removed after compiling.";
            CoherenceHubLayout.DrawInfoLabel(new GUIContent(
                $"Simulator build for your active build target. {symbolMessage}"));
            CoherenceHubLayout.DrawMessageArea(new GUIContent("You can use this build to test it locally."));
            EditorGUILayout.Space();

            var simBuildAbility = GetSimBuildBlockers(buildOptions);
            DrawLocalSimulatorBuildButton(simBuildAbility.HasFlag(SimulatorBuildBlocker.HasNoScenes) ||
                                          simBuildAbility.HasFlag(SimulatorBuildBlocker.EditorImportingOrCompiling) ||
                                          SessionState.GetBool(SimulatorBuildPipeline.ServerModuleMissingKey, false));
            DrawLocalBuildErrorMessages(simBuildAbility.HasFlag(SimulatorBuildBlocker.HasNoScenes));
        }

        internal static void DrawAutoSimulatorConnection()
        {
            CoherenceHubLayout.DrawMessageArea(
                new GUIContent("Add this Component to let Simulators connect to RS automatically."));
            EditorGUILayout.Space();

            if (GUILayout.Button("Add AutoSimulatorConnection"))
            {
                Utils.AddAutoSimulatorConnection(new(null));
            }
        }

        internal static void DrawRunSimulatorSettings(ref EndpointData endpoint)
        {
            DrawExecutablePath();

            EditorGUILayout.Space();

            DrawReplicationServerDataHeader(ref endpoint);

            EditorGUILayout.Space();

            InitRegionAndSimType(ref endpoint);

            EditorGUI.BeginChangeCheck();

            endpoint.region = selectedRegion;
            endpoint.simulatorType = EditorGUILayout.EnumPopup(new GUIContent("Type"),
                ParseEnumValue(typeof(EndpointData.SimulatorType), endpoint.simulatorType)).ToString();
            endpoint.host = CoherenceHubLayout.DrawTextField("Host", endpoint.host);
            endpoint.port = CoherenceHubLayout.DrawIntField("Port", endpoint.port);
            endpoint.worldId = UInt64.Parse(CoherenceHubLayout.DrawTextField("World Id", endpoint.worldId.ToString()));
            endpoint.roomId = (ushort)CoherenceHubLayout.DrawIntField("Room Id", endpoint.roomId);

            if (!ulong.TryParse(CoherenceHubLayout.DrawTextField("Unique Room Id", endpoint.uniqueRoomId.ToString()),
                    out endpoint.uniqueRoomId))
            {
                endpoint.uniqueRoomId = 0;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SimulatorEditorUtility.SaveEndPoint(endpoint);
            }

            DrawRunLocalSimulatorButton(endpoint);

            DrawRunSimErrorMessages(endpoint);
        }

        private static void InitRegionAndSimType(ref EndpointData endpoint)
        {
            if (string.IsNullOrEmpty(endpoint.region))
            {
                endpoint.region = EndpointData.LocalRegion;
            }

            if (string.IsNullOrEmpty(endpoint.simulatorType))
            {
                endpoint.simulatorType = EndpointData.SimulatorType.world.ToString();
            }
        }

        private static Enum ParseEnumValue(Type type, string value)
        {
            return (Enum)Enum.Parse(type, value);
        }

        private static bool IsRegionSupported(string region) => region.Equals(EndpointData.LocalRegion);

        private static void DrawRunLocalSimulatorButton(EndpointData endpoint)
        {
            EditorGUI.BeginDisabledGroup(!SimulatorEditorUtility.CanRunLocalSimulator(endpoint));
            using (new EditorGUILayout.HorizontalScope())
            {
                var content = Icons.GetContentWithText("Coherence.Terminal", "Run Local Simulator",
                    "Execute a local simulator Unity build, and automatically connect to the given Replication Server.");
                if (GUILayout.Button(content, EditorStyles.miniButtonLeft))
                {
                    SimulatorEditorUtility.RunLocalSimulator(SimulatorEditorUtility.LocalSimulatorExecutablePath,
                        endpoint);
                }

                if (GUILayout.Button(ContentUtils.GUIContents.clipboard, EditorStyles.miniButtonRight,
                        GUILayout.Width(22)))
                {
                    GUIUtility.systemCopyBuffer = SimulatorEditorUtility.GetLocalSimCommand(
                        SimulatorEditorUtility.LocalSimulatorExecutablePath,
                        endpoint);
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent("Copied to Clipboard"));
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawReplicationServerDataHeader(ref EndpointData endpoint)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawBoldLabel(new GUIContent("Replication Server endpoint"));
                GUILayout.FlexibleSpace();

                var isValidEndpoint = SimulatorEditorUtility.LastUsedEndpoint.Validate(false).isValid;
                var isValidRegion = !string.IsNullOrEmpty(endpoint.region) && IsRegionSupported(endpoint.region);
                var tooltip = isValidEndpoint switch
                {
                    true => "The local RS Endpoint Data will be automatically fetched from the last RS you connected to at runtime.",
                    _ when !isValidRegion => $"Region {endpoint.region} is not supported right now. Stay tuned for updates!",
                    _ => "No last used local RS endpoint data available. You can fetch the last used RS Endpoint Data from the last local RS you connected to at runtime."
                };

                var guiContent = new GUIContent("Fetch", tooltip);

                EditorGUI.BeginDisabledGroup(!(isValidEndpoint && isValidRegion));
                if (CoherenceHubLayout.DrawButton(guiContent))
                {
                    endpoint = SimulatorEditorUtility.LastUsedEndpoint;
                    endpoint.host = endpoint.host.Trim();

                    if (endpoint.worldId != 0)
                    {
                        endpoint.simulatorType = EndpointData.SimulatorType.world.ToString();
                    }
                    else if (endpoint.roomId != 0 || endpoint.uniqueRoomId != 0)
                    {
                        endpoint.simulatorType = EndpointData.SimulatorType.room.ToString();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private static void DrawExecutablePath()
        {
            var previousPath = SimulatorEditorUtility.LocalSimulatorExecutablePath;
            CoherenceHubLayout.DrawDiskPath(SimulatorEditorUtility.LocalSimulatorExecutablePath,
                "Select a valid Simulator executable file.", OpenFilePanel, newPath =>
                {
                    newPath = FixMacOsPath(newPath);
                    SimulatorEditorUtility.LocalSimulatorExecutablePath = newPath;
                });
        }

        private static string FixMacOsPath(string newPath)
        {
            if (!newPath.EndsWith(".app"))
            {
                return newPath;
            }

            var pathCandidate = $"{newPath}/Contents/MacOS/{SimulatorBuildOptions.BuildName}";

            if (!File.Exists(pathCandidate))
            {
                pathCandidate = $"{newPath}/Contents/MacOS/{PlayerSettings.productName}";
            }

            newPath = pathCandidate;

            return newPath;
        }

        private static string OpenFilePanel()
        {
            return EditorUtility.OpenFilePanel("Select Simulator Build Executable", string.Empty, GetExtension());
        }

        private static string GetExtension()
        {
#if UNITY_EDITOR_WIN
            return "exe";
#else
            return string.Empty;
#endif
        }

        private static void DrawRunSimErrorMessages(EndpointData endpoint)
        {
            if (!SimulatorEditorUtility.ValidateIpAndPort(endpoint))
            {
                CoherenceHubLayout.DrawWarnArea("Endpoint is missing host or port");
            }

            if (!SimulatorEditorUtility.ValidateIp(endpoint.host))
            {
                CoherenceHubLayout.DrawWarnArea("Endpoint IP is invalid");
            }

            if (!SimulatorEditorUtility.BuildPathHasSimulatorBuild())
            {
                CoherenceHubLayout.DrawWarnArea("Build path doesn't contain a Simulator build");
            }
        }

        private static void DrawSimSlug()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                CoherenceHubLayout.DrawPrefixLabel(new GUIContent("Simulator Slug",
                    "The simulator slug is a string that uniquely identifies this upload. It can be used later when deploying world or room simulators."));
                RuntimeSettings.Instance.SimulatorSlug =
                    CoherenceHubLayout.DrawTextField(RuntimeSettings.Instance.SimulatorSlug);
                if (EditorGUI.EndChangeCheck())
                {
                    SaveProjectSlug(RuntimeSettings.Instance.ProjectID, RuntimeSettings.Instance.SimulatorSlug);
                }
            }
        }

        private static void DrawOrgAndProject()
        {
            var orgName = GetOrgName(RuntimeSettings.Instance.OrganizationID);
            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawPrefixLabel(new GUIContent("Organization"));
                CoherenceHubLayout.DrawLabel(new GUIContent(orgName));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawPrefixLabel(new GUIContent("Project"));
                CoherenceHubLayout.DrawLabel(new GUIContent(RuntimeSettings.Instance.ProjectName));
            }
        }

        private static void SaveProjectSlug(string projectID, string simSlug)
        {
            if (string.IsNullOrEmpty(projectID))
            {
                return;
            }

            ProjectSimulatorSlugStore.Set(projectID, simSlug);
        }

        private static void DrawCreateAndUploadInformation(SimulatorBuildBlocker simulatorBuildBlocker)
        {
            var isLinux = EditorUserBuildSettings.selectedStandaloneTarget == BuildTarget.StandaloneLinux64;

            if (!isLinux)
            {
                CoherenceHubLayout.DrawMessageArea("Linux platform");
            }

            var isServerSubTarget =
                EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server;

            if (!isServerSubTarget && simulatorBuildBlocker.HasFlag(SimulatorBuildBlocker.LinuxDedicatedServerBuildSupportInstalled))
            {
                CoherenceHubLayout.DrawMessageArea("Standalone: Server module");
            }
            else if (isServerSubTarget & !simulatorBuildBlocker.HasFlag(SimulatorBuildBlocker.LinuxDedicatedServerBuildSupportInstalled))
            {
                CoherenceHubLayout.DrawMessageArea("Standalone: Player");
            }

            if (simulatorBuildBlocker.HasFlag(SimulatorBuildBlocker.CoherenceSimulatorDirectiveMissing))
            {
                CoherenceHubLayout.DrawMessageArea("COHERENCE_SIMULATOR scripting symbol");
            }
        }

        private static bool IsLocalBuildSymbolDefined(SimulatorBuildOptions buildOptions)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            var namedBuildTarget = buildOptions.HeadlessMode
                ? NamedBuildTarget.Server
                : NamedBuildTarget.FromBuildTargetGroup(group);
            return PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget)
                .Contains("COHERENCE_SIMULATOR");
        }

        private static void DrawCreateAndUploadErrorMessages(SimulatorBuildBlocker simulatorBuildBlocker,
            ScriptingImplementation scriptingImplementation)
        {
            if (simulatorBuildBlocker.HasFlag(SimulatorBuildBlocker.HasNoScenes))
            {
                CoherenceHubLayout.DrawErrorArea(AtLeastOneSceneWarning);
            }

            if (!PortalUtil.CanCommunicateWithPortal)
            {
                CoherenceHubLayout.DrawErrorArea("Log in to be able to upload builds");
            }

            if (string.IsNullOrEmpty(RuntimeSettings.Instance.OrganizationID))
            {
                CoherenceHubLayout.DrawErrorArea("Organization not set. Open the 'coherence Cloud' tab in the coherence hub and select organization from the dropdown.");
            }

            if (string.IsNullOrEmpty(RuntimeSettings.Instance.ProjectID))
            {
                CoherenceHubLayout.DrawErrorArea("Project not set. Open the 'coherence Cloud' tab in the coherence hub and select project from the dropdown.");
            }

            if (string.IsNullOrEmpty(RuntimeSettings.Instance.SimulatorSlug))
            {
                CoherenceHubLayout.DrawErrorArea("Simulator Slug string is empty");
            }

            if (!simulatorBuildBlocker.HasFlag(SimulatorBuildBlocker.LinuxBuildSupportInstalled) ||
                !simulatorBuildBlocker.HasFlag(SimulatorBuildBlocker.LinuxDedicatedServerBuildSupportInstalled))
            {
                if (simulatorBuildBlocker.HasFlag(SimulatorBuildBlocker.LinuxDedicatedServerBuildSupportInstalled))
                {
                    CoherenceHubLayout.DrawErrorArea(
                        $"Linux build not supported for {scriptingImplementation}, install the Linux modules.");
                }
                else
                {
                    CoherenceHubLayout.DrawWarnArea(
                        $"Headless build not supported for {scriptingImplementation}, install the Linux Dedicated Server module");
                }
            }

            EditorImportingOrCompilingWarning();
        }

        private static void DrawLocalBuildErrorMessages(bool sceneRequired)
        {
            if (SessionState.GetBool(SimulatorBuildPipeline.ServerModuleMissingKey, false))
            {
                var target = EditorUserBuildSettings.activeBuildTarget switch
                {
                    BuildTarget.StandaloneLinux64 => "Linux",
                    BuildTarget.StandaloneOSX => "MacOS",
                    BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneWindows => "Windows",
                    _ => NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                        .TargetName
                };
                CoherenceHubLayout.DrawErrorArea(
                    $"Standalone Server module is missing. Please install the {target} stand alone build support module.");
            }

            if (sceneRequired)
            {
                CoherenceHubLayout.DrawErrorArea(AtLeastOneSceneWarning);
            }

            EditorImportingOrCompilingWarning();
        }

        private static void EditorImportingOrCompilingWarning()
        {
            if (IsEditorImportingOrCompiling())
            {
                CoherenceHubLayout.DrawWarnArea(
                    "Cannot build player while editor is importing assets or compiling scripts.");
            }
        }

        private static bool IsEditorImportingOrCompiling()
        {
            return EditorApplication.isCompiling || EditorApplication.isUpdating;
        }

        private static void DrawCreateAndUploadBuildButton(bool disabled)
        {
            using var scope = new EditorGUI.DisabledScope(disabled);
            using (new EditorGUILayout.HorizontalScope())
            {
                var tooltip = disabled ? "Fix errors below." : "";
                CoherenceHubLayout.DrawCloudDependantButton(new GUIContent("Build And Upload Simulator"),
                    () =>
                    {
                        SimulatorBuildPipeline.BuildHeadlessLinuxClientAsync();
                        GUIUtility.ExitGUI();
                    }, tooltip, null, EditorStyles.miniButtonLeft);

                if (GUILayout.Button(SimulatorsModule.ModuleGUIContents.HeadlessBuildExecutable,
                        EditorStyles.miniButtonRight,
                        GUILayout.Width(22)))
                {
                    var path = Shellscript.ExportSimulatorBuildCommand();
                    EditorUtility.RevealInFinder(path);
                }
            }
        }

        private static SimulatorBuildBlocker GetSimBuildBlockers(SimulatorBuildOptions buildOptions)
        {
            var error = SimulatorBuildBlocker.None;

            if (string.IsNullOrEmpty(RuntimeSettings.Instance.SimulatorSlug))
            {
                error |= SimulatorBuildBlocker.NoSimulatorSlugProvided;
            }

            if (IsEditorImportingOrCompiling())
            {
                error |= SimulatorBuildBlocker.EditorImportingOrCompiling;
            }

            if (EditorBuildSettings.scenes.Length == 0)
            {
                error |= SimulatorBuildBlocker.NoScenesInBuildOptions;
            }

            if (!buildOptions.HasAnyScenes)
            {
                error |= SimulatorBuildBlocker.HasNoScenes;
            }

            if (SimulatorEditorUtility.IsBuildTargetSupported(true, buildOptions.ScriptingImplementation))
            {
                error |= SimulatorBuildBlocker.LinuxDedicatedServerBuildSupportInstalled;
            }

            if (SimulatorEditorUtility.IsBuildTargetSupported(false, buildOptions.ScriptingImplementation))
            {
                error |= SimulatorBuildBlocker.LinuxBuildSupportInstalled;
            }

            if (string.IsNullOrEmpty(RuntimeSettings.Instance.OrganizationID))
            {
                error |= SimulatorBuildBlocker.OrganizationIdMissing;
            }

            if (string.IsNullOrEmpty(RuntimeSettings.Instance.ProjectID))
            {
                error |= SimulatorBuildBlocker.ProjectIdMissing;
            }

            if (PlayerSettings
                .GetScriptingDefineSymbols(error.HasFlag(SimulatorBuildBlocker.LinuxDedicatedServerBuildSupportInstalled)
                    ? NamedBuildTarget.Server
                    : NamedBuildTarget.Standalone).Contains("COHERENCE_SIMULATOR"))
            {
                error |= SimulatorBuildBlocker.CoherenceSimulatorDirectiveMissing;
            }

            if (error.HasFlag(SimulatorBuildBlocker.HasNoScenes) ||
                error.HasFlag(SimulatorBuildBlocker.NoSimulatorSlugProvided) ||
                error.HasFlag(SimulatorBuildBlocker.OrganizationIdMissing) ||
                error.HasFlag(SimulatorBuildBlocker.ProjectIdMissing) ||
                !error.HasFlag(SimulatorBuildBlocker.LinuxInstalled))
            {
                error |= SimulatorBuildBlocker.CannotUploadLinuxBuild;
            }

            return error;
        }

        private static void DrawLocalSimulatorBuildButton(bool disabled)
        {
            using (new EditorGUI.DisabledScope(disabled))
            {
                var forcePrompt = Event.current.alt;
                var hasPreviousPath =
                    !string.IsNullOrEmpty(SessionState.GetString(SimulatorBuildPipeline.PreviousLocalBuildPathKey,
                        string.Empty));
                var buildText = hasPreviousPath && !forcePrompt ? "Rebuild" : "Build";
                var altKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Option" : "Alt";
                var buildSimulatorLabel =
                    new GUIContent($"{buildText} Local Simulator ({EditorUserBuildSettings.activeBuildTarget})")
                    {
                        tooltip = hasPreviousPath
                            ? $"{buildText} the Local Simulator for {EditorUserBuildSettings.activeBuildTarget}. Hold down the {altKey} key when clicking this button to specify a new build path."
                            : "Build a simulator to test locally.",
                    };

                if (CoherenceHubLayout.DrawButton(buildSimulatorLabel))
                {
                    if (!SessionState.GetBool(SimulatorBuildPipeline.UserConfirmedLocalServer, false))
                    {
                        var userConfirmed = EditorUtility.DisplayDialog("Local Server Build Module",
                            "Are you sure you have server build module installed for your platform?", "Yes",
                            "No");
                        if (userConfirmed)
                        {
                            SessionState.SetBool(SimulatorBuildPipeline.UserConfirmedLocalServer, true);
                        }
                        else
                        {
                            return;
                        }
                    }

                    // If the user holds down alt, show the build location dialog
                    SimulatorBuildPipeline.BuildLocalSimulator(forcePrompt);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static bool HasOrgAndProject() => !string.IsNullOrEmpty(RuntimeSettings.Instance.OrganizationID) &&
                                                  !string.IsNullOrEmpty(RuntimeSettings.Instance.ProjectID);

        private static string GetOrgName(string orgId) =>
            PortalLogin.organizations.FirstOrDefault(o => o.id == orgId)?.name ?? "-";
    }
}
