// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Cloud;
    using Coherence.Toolkit;
    using Log;
    using Portal;
    using ReplicationServer;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    [CustomEditor(typeof(ProjectSettings))]
    internal class ProjectSettingsEditor : Editor
    {
        private SerializedProperty worldUDPPort;
        private SerializedProperty worldWebPort;
        private SerializedProperty roomsUDPPort;
        private SerializedProperty roomsWebPort;
        private SerializedProperty sendFrequency;
        private SerializedProperty recvFrequency;
        private SerializedProperty localRoomsCleanupTimeSeconds;
        private SerializedProperty localWorldHostAuthority;
        private SerializedProperty rsConsoleLogLevel;
        private SerializedProperty rsLogToFile;
        private SerializedProperty rsLogFilePath;
        private SerializedProperty rsFileLogLevel;
        private SerializedProperty keepConnectionAlive;
        private SerializedProperty reportAnalytics;

        private RuntimeSettings runtimeSettings;
        private SerializedObject runtimeSettingsSerializedObject;
        private SerializedProperty localDevelopmentMode;
        private SerializedProperty useNativeCore;
        private SerializedProperty defaultAuthorityTransferType;

        private SerializedProperty showHubModuleQuickHelp;
        private SerializedProperty bundleRs;

        private string bakeFolder;

        private EditorWindow projectSettingsWindow;

        private bool lastAdvanced;
        private bool skipLongUnitTests;

        private Settings logSettings;

        private enum Mode
        {
            Rooms,
            Worlds
        }

        private Mode mode;
        private const string modeSessionKey = "Coherence.Settings.Mode";
        private bool advanced;

        private class GUIContents
        {
            public static readonly GUIContent port = EditorGUIUtility.TrTextContent("Port");
            public static readonly GUIContent webPort = EditorGUIUtility.TrTextContent("Web Port", "Port used by default on WebGL builds.");
            public static readonly GUIContent localDevelopmentMode = EditorGUIUtility.TrTextContent("Local Development Mode", "Allows development features like localhost replication server discovery and multi-room simulators local forwarding. Disable this on release/distributable builds.");
            public static readonly GUIContent replicationServerTitle = EditorGUIUtility.TrTextContent("Local Replication Server");
            public static readonly GUIContent keepConnectionAliveLabel = EditorGUIUtility.TrTextContent("Disable Connection Timeouts (Editor)", "Sets the '--disconnect-timeout' Replication Server flag to its max value.\n\nEven if the Editor is not actively updating, the connection won't time out.");
            public static readonly GUIContent reportAnalytics = EditorGUIUtility.TrTextContent("Share Anonymous Analytics");
            public static readonly GUIContent advanced = EditorGUIUtility.TrTextContent("Advanced");
            public static readonly GUIContent useCustomTools = EditorGUIUtility.TrTextContent("Use Custom Tools");
            public static readonly GUIContent useCustomEndpoints = EditorGUIUtility.TrTextContent("Use Custom Endpoints");
            public static readonly GUIContent customToolsPath = EditorGUIUtility.TrTextContent("Custom Tools Path");
            public static readonly GUIContent customAPIDomain = EditorGUIUtility.TrTextContent("Custom API Domain");
            public static readonly GUIContent useNativeCore = EditorGUIUtility.TrTextContent("Use Native Client Core");
            public static readonly GUIContent defaultAuthorityTransferType = new("Default Authority Transfer Type", "The default Authority Transfer Type for CoherenceSyncs. Useful to change from Steal when using Host Authority restrictions.");
            public static readonly GUIContent consoleLogLevel = EditorGUIUtility.TrTextContent("Console Level (Editor)");
            public static readonly GUIContent rsConsoleLogLevel = EditorGUIUtility.TrTextContent("Console Level (Editor)");
            public static readonly GUIContent consoleLogFilter = EditorGUIUtility.TrTextContent("Filter", "Comma-separated list of terms.\n\nEach logger is associated with a source. Logger sources (type names) that contain (include) or don't contain (exclude) the terms specified in this filter, will be logged.");
            public static readonly GUIContent editorLogLevel = EditorGUIUtility.TrTextContent("Editor.log Level");
            public static readonly GUIContent logStackTrace = EditorGUIUtility.TrTextContent("Include Stack Trace");
            public static readonly GUIContent logFilePath = EditorGUIUtility.TrTextContent("Path");
            public static readonly GUIContent logToFile = EditorGUIUtility.TrTextContent("Write to File");
            public static readonly GUIContent rsLogToFile = EditorGUIUtility.TrTextContent("Write to File");
            public static readonly GUIContent logLevel = EditorGUIUtility.TrTextContent("Level");
            public static readonly GUIContent showHubQuickHelp = EditorGUIUtility.TrTextContent("Show Quick Help on Hub");
            public static readonly GUIContent logsTitle = EditorGUIUtility.TrTextContent("Unity Logs");
            public static readonly GUIContent bundleReplicationServer = EditorGUIUtility.TrTextContent("Bundle In Build");
            public static readonly GUIContent localWorldHostAuthority = new("Host Authority Restrictions", "When starting a local World, apply restrictions so that only Simulators and Hosts are allowed to perform the specified actions.");
            public static readonly GUIContent localRoomHostAuthorityHelp = new($"If you want to set host authority restrictions, you need to pass them at room creation time via {nameof(SelfHostedRoomCreationOptions)}.{nameof(SelfHostedRoomCreationOptions.HostAuthority)}.");
            public static readonly GUIContent automationsTitle = EditorGUIUtility.TrTextContent("Automations");
            public static readonly GUIContent uploadSchemasTitle = EditorGUIUtility.TrTextContent("Upload Schemas to Cloud");
            public static readonly GUIContent bakeAutomationsTitle = EditorGUIUtility.TrTextContent("Bake");
            public static readonly GUIContent featureFlagsTitle = EditorGUIUtility.TrTextContent("Feature Flags");
            public static readonly GUIContent scriptingDefineHelp = EditorGUIUtility.TrTextContent("Added only for the active build target");
            public static readonly GUIContent rsLogsTitle = EditorGUIUtility.TrTextContent("Local Replication Server Logs");

#if UNITY_EDITOR_OSX
            public static readonly GUIContent altToAdvanced = EditorGUIUtility.TrTextContent("Hold ⌥ Option to show advanced options.");
#else
            public static readonly GUIContent altToAdvanced = EditorGUIUtility.TrTextContent("Hold Alt to show advanced options.");
#endif

#if UNITY_EDITOR_OSX
            public static readonly GUIContent altToBuildLogs = EditorGUIUtility.TrTextContent("Hold ⌥ Option to show log settings on builds.");
#else
            public static readonly GUIContent altToBuildLogs = EditorGUIUtility.TrTextContent("Hold Alt to show log settings on builds.");
#endif

#if UNITY_EDITOR_OSX
            public static readonly GUIContent showInExplorer = EditorGUIUtility.TrTextContent("Reveal in Finder");
#else
            public static readonly GUIContent showInExplorer = EditorGUIUtility.TrTextContent("Show in Explorer");
#endif
        }

        private void RepaintWindow()
        {
            if (!projectSettingsWindow)
            {
                return;
            }

            projectSettingsWindow.Repaint();
        }

        private void OnEnable()
        {
            // make sure ProjectSettings are editable
            ProjectSettings.instance.hideFlags &= ~HideFlags.NotEditable;

            var t = Type.GetType("UnityEditor.SettingsWindow,UnityEditor.dll");
            if (t != null)
            {
                var windows = Resources.FindObjectsOfTypeAll(t);
                if (windows.Length > 0)
                {
                    projectSettingsWindow = Resources.FindObjectsOfTypeAll(t)[0] as EditorWindow;
                }
            }

            EditorApplication.modifierKeysChanged += RepaintWindow;
            EditorApplication.projectChanged += OnProjectChanged;

            worldUDPPort = serializedObject.FindProperty("worldUDPPort");
            worldWebPort = serializedObject.FindProperty("worldWebPort");
            roomsUDPPort = serializedObject.FindProperty("roomsUDPPort");
            roomsWebPort = serializedObject.FindProperty("roomsWebPort");
            sendFrequency = serializedObject.FindProperty("sendFrequency");
            recvFrequency = serializedObject.FindProperty("recvFrequency");
            localRoomsCleanupTimeSeconds = serializedObject.FindProperty("localRoomsCleanupTimeSeconds");
            localWorldHostAuthority = serializedObject.FindProperty(nameof(ProjectSettings.localWorldHostAuthority));
            rsConsoleLogLevel = serializedObject.FindProperty(nameof(ProjectSettings.rsConsoleLogLevel));
            rsLogToFile = serializedObject.FindProperty(nameof(ProjectSettings.rsLogToFile));
            rsLogFilePath = serializedObject.FindProperty(nameof(ProjectSettings.rsLogFilePath));
            rsFileLogLevel = serializedObject.FindProperty(nameof(ProjectSettings.rsFileLogLevel));
            keepConnectionAlive = serializedObject.FindProperty("keepConnectionAlive");
            reportAnalytics = serializedObject.FindProperty("reportAnalytics");
            showHubModuleQuickHelp = serializedObject.FindProperty(nameof(ProjectSettings.showHubModuleQuickHelp));
            bundleRs = serializedObject.FindProperty(nameof(ProjectSettings.RSBundlingEnabled));

            Refresh();

            mode = (Mode)SessionState.GetInt(modeSessionKey, 0);
            skipLongUnitTests = DefinesManager.IsSkipLongTestsDefineEnabled();

            logSettings = Log.GetSettings();
        }

        private void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= RepaintWindow;
            EditorApplication.projectChanged -= OnProjectChanged;
            PortalLogin.StopPolling();
        }

        private void OnProjectChanged()
        {
            Refresh();
            Repaint();
        }

        private void Refresh()
        {
            ProjectSettings.instance.PruneSchemas();

            runtimeSettings = RuntimeSettings.Instance;

            if (runtimeSettingsSerializedObject != null)
            {
                runtimeSettingsSerializedObject.Dispose();
                runtimeSettingsSerializedObject = null;
            }

            if (runtimeSettings)
            {
                runtimeSettingsSerializedObject = new SerializedObject(runtimeSettings);
                localDevelopmentMode = runtimeSettingsSerializedObject.FindProperty("localDevelopmentMode");
                useNativeCore = runtimeSettingsSerializedObject.FindProperty("useNativeCore");
                defaultAuthorityTransferType = runtimeSettingsSerializedObject.FindProperty(nameof(RuntimeSettings.defaultAuthorityTransferType));
            }

            if (!string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID))
            {
                Schemas.UpdateSyncState();
            }

            if (!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken))
            {
                PortalLogin.FetchOrgs();
            }
        }

        public override void OnInspectorGUI()
        {
            ContentUtils.DrawCloneModeMessage();

            EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
            serializedObject.Update();

            advanced = Event.current.modifiers == EventModifiers.Alt;

            if (lastAdvanced != advanced)
            {
                GUIUtility.keyboardControl = 0;
                lastAdvanced = advanced;
            }

            DrawMiscSettings();

            EditorGUILayout.Space();

            DrawAutomations();

            EditorGUILayout.Space();

            DrawLocalReplicationServer();

            DrawMissingRuntimeSettings();

            EditorGUI.EndDisabledGroup();

            DrawLogs();
            DrawRsLogs();

            EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);

            if (advanced ||
                ProjectSettings.instance.UseCustomTools ||
                ProjectSettings.instance.UseCustomEndpoints ||
                rsLogToFile.boolValue ||
                logSettings.LogToFile)
            {
                DrawAdvancedSettings();
            }
            else
            {
                EditorGUILayout.LabelField(GUIContents.altToAdvanced, ContentUtils.GUIStyles.miniLabelGrey);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                ProjectSettings.instance.Save();
            }

            if (runtimeSettingsSerializedObject != null)
            {
                _ = runtimeSettingsSerializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawAutomations()
        {
            EditorGUILayout.LabelField(GUIContents.automationsTitle, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(GUIContents.bakeAutomationsTitle);
            EditorGUI.indentLevel++;
            BakeUtil.BakeOnEnterPlayMode = EditorGUILayout.Toggle("On Enter Play Mode", BakeUtil.BakeOnEnterPlayMode);
            BakeUtil.BakeOnBuild = EditorGUILayout.Toggle("On Unity Player Build", BakeUtil.BakeOnBuild);
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(GUIContents.uploadSchemasTitle);
            EditorGUI.indentLevel++;
            PortalUtil.UploadOnEnterPlayMode = EditorGUILayout.Toggle("On Enter Play Mode", PortalUtil.UploadOnEnterPlayMode);
            PortalUtil.UploadOnBuild = EditorGUILayout.Toggle("On Unity Player Build", PortalUtil.UploadOnBuild);
            PortalUtil.UploadAfterBake = EditorGUILayout.Toggle("On Baking Complete", PortalUtil.UploadAfterBake);
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        private void DrawAdvancedSettings()
        {
            // advanced
            EditorGUILayout.LabelField(GUIContents.advanced, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // feature flags
            EditorGUILayout.LabelField(GUIContents.featureFlagsTitle, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawScriptingDefine(FeatureFlags.MultiRoomSimulator);
            EditorGUILayout.LabelField(GUIContents.scriptingDefineHelp, ContentUtils.GUIStyles.miniLabelGreyWrap);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.Space();

            // native client core
            if (useNativeCore != null)
            {
                _ = EditorGUILayout.PropertyField(useNativeCore, GUIContents.useNativeCore);
            }

            // custom tools
            EditorGUI.BeginChangeCheck();
            var uct = EditorGUILayout.Toggle(GUIContents.useCustomTools, ProjectSettings.instance.UseCustomTools);
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettings.instance.UseCustomTools = uct;
                if (uct && string.IsNullOrEmpty(ProjectSettings.instance.CustomToolsPath))
                {
                    var p = Environment.GetEnvironmentVariable("GOPATH");
                    ProjectSettings.instance.CustomToolsPath =
                        p != null ? Path.Combine(p, "bin") : Paths.nativeToolsPath;
                }
            }

            EditorGUI.BeginDisabledGroup(!ProjectSettings.instance.UseCustomTools);
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var path = EditorGUILayout.TextField(GUIContents.customToolsPath, ProjectSettings.instance.CustomToolsPath);
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettings.instance.CustomToolsPath = path;
            }

            if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                var folder = EditorUtility.OpenFolderPanel("Select Custom Tools Path", ProjectSettings.instance.CustomToolsPath, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    ProjectSettings.instance.CustomToolsPath = folder;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(ProjectSettings.instance.CustomToolsPath))
            {
                if (!File.Exists(Path.Combine(ProjectSettings.instance.CustomToolsPath, Paths.replicationServerName)))
                {
                    EditorGUILayout.HelpBox(
                        $"'{ProjectSettings.instance.CustomToolsPath}' does not contain a binary called '{Paths.replicationServerName}'.",
                        MessageType.Warning);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorLauncher.StartInTerminal =
                EditorGUILayout.Toggle("Use Terminal as RS Host", EditorLauncher.StartInTerminal);
            // custom endpoint toggle

            EditorGUI.BeginChangeCheck();
            var use = EditorGUILayout.Toggle(GUIContents.useCustomEndpoints,
                ProjectSettings.instance.UseCustomEndpoints);
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettings.instance.UseCustomEndpoints = use;
                if (use && string.IsNullOrEmpty(ProjectSettings.instance.CustomAPIDomain))
                {
                    ProjectSettings.instance.CustomAPIDomain = Endpoints.apiDomain;
                }
            }

            // custom endpoints

            EditorGUI.BeginDisabledGroup(!ProjectSettings.instance.UseCustomEndpoints);
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            var apiDomain =
                EditorGUILayout.TextField(GUIContents.customAPIDomain, ProjectSettings.instance.CustomAPIDomain);
            if (EditorGUI.EndChangeCheck())
            {
                if (apiDomain.StartsWith("https://"))
                {
                    apiDomain = apiDomain.Substring(8);
                    if (apiDomain.IndexOf("/") > -1)
                    {
                        apiDomain = apiDomain.Substring(0, apiDomain.IndexOf("/"));
                    }
                }

                ProjectSettings.instance.CustomAPIDomain = apiDomain;
            }

            if (GUILayout.Button("local", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                ProjectSettings.instance.CustomAPIDomain = "localhost";
            }

            if (GUILayout.Button("stage", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                ProjectSettings.instance.CustomAPIDomain = "api.stage.coherence.io";
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            // skipping long tests
            EditorGUI.BeginChangeCheck();
            skipLongUnitTests = EditorGUILayout.Toggle("Skip Long Unit Tests", skipLongUnitTests);
            if (EditorGUI.EndChangeCheck())
            {
                DefinesManager.ApplySkipLongUnitTestsDefine(skipLongUnitTests);
            }

            // doc generation

            if (Directory.Exists(Paths.docFxPath))
            {
                EditorGUILayout.LabelField("API Docs", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("DocFX v2.78.3+ required.", MessageType.Info);
                EditorGUILayout.HelpBox("Make sure your .csprojs are generated and up-to-date. Go to Preferences > External Tools, disable \"Player projects\" and hit \"Regenerate project files\". Then, execute the following steps in order.", MessageType.Info);
                if (File.Exists("Assets/csc.rsp"))
                {
                    EditorGUILayout.HelpBox(
                        "csc.rsp can interfere with docs generation.",
                        MessageType.Warning);
                }

                if (ContentUtils.DrawIndentedButton($"Create {Paths.directoryBuildTargetsFile}"))
                {
                    if (!DocGenUtil.HasDirectoryBuildTargets ||
                        (DocGenUtil.HasDirectoryBuildTargets &&
                        EditorUtility.DisplayDialog(Paths.directoryBuildTargetsFile,
                            $"{Paths.directoryBuildTargetsFile} file exists. Override?",
                            "OK", "Cancel")))
                    {
                        DocGenUtil.GenerateDirectoryBuildTargets();
                        ShowNotification("BuildTargets created");
                    }
                }

                if (ContentUtils.DrawIndentedButton("Build XMLs"))
                {
                    DocGenUtil.RunBuildSolution();
                    ShowNotification("Running on terminal");
                }

                if (ContentUtils.DrawIndentedButton("Build Metadata"))
                {
                    DocGenUtil.FetchBuildArtifacts();
                    ShowNotification("Metadata built");
                }

                if (ContentUtils.DrawIndentedButton("Build & Serve Site" + (advanced ? " (DEBUG)" : "")))
                {
                    DocGenUtil.RunDocFx(advanced);
                    ShowNotification("Running on terminal");
                }

                EditorGUILayout.Space();

                if (ContentUtils.DrawIndentedButton("Open DocFX Folder"))
                {
                    EditorUtility.RevealInFinder(Paths.docFxConfigPath);
                }

                if (ContentUtils.DrawIndentedButton("Clear All"))
                {
                    if (!EditorUtility.DisplayDialog("Clear All", "Are you sure you want to clear _site and _site.zip?",
                            "Clear All", "Cancel"))
                    {
                        GUIUtility.ExitGUI();
                        return;
                    }

                    var zipPath = Path.GetFullPath(Paths.docFxSiteZipPath);
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }

                    var sitePath = Path.GetFullPath(Paths.docFxSitePath);
                    if (Directory.Exists(sitePath))
                    {
                        Directory.Delete(sitePath, recursive: true);
                    }

                    ShowNotification("Cleared");
                }

                if (ContentUtils.DrawIndentedButton("Zip compiled site"))
                {
                    var sitePath = Path.GetFullPath(Paths.docFxSitePath);
                    if (!File.Exists(sitePath))
                    {
                        Debug.LogError("No site found. Did you build it?");
                        GUIUtility.ExitGUI();
                        return;
                    }

                    EditorUtility.DisplayProgressBar("Compressing documentation site", "Compressing...", 1f);
                    try
                    {
                        var zipPath = Path.GetFullPath(Paths.docFxSiteZipPath);
                        if (File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
                        }
                        ZipUtils.Zip(Path.GetFullPath(Paths.docFxSitePath), Path.GetFullPath(Paths.docFxSiteZipPath), uploadAll: false);

                        if (File.Exists(zipPath))
                        {
                            EditorUtility.RevealInFinder(zipPath);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // baking
            EditorGUILayout.LabelField("Baking", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!Directory.Exists(Paths.defaultSchemaBakePath));
            if (ContentUtils.DrawIndentedButton("Delete Bake Files"))
            {
                CodeGenSelector.Clear(true);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (ContentUtils.DrawIndentedButton("Bake Wizard"))
            {
                AdvancedBakeWizard.Open();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            if (ContentUtils.DrawIndentedButton("Select HubModuleManager"))
            {
                Selection.activeObject = HubModuleManager.instance;
            }

            if (ContentUtils.DrawIndentedButton("Log GitBook URLs"))
            {
                var urls = DocumentationLinks.ActiveKeys
                    .ToDictionary(key => key, DocumentationLinks.GetDocsUrl)
                    .Select(pair => $"{pair.Key}\n<a href=\"{pair.Value}\">{pair.Value}</a>");

                var documentedTypes = TypeCache
                    .GetTypesWithAttribute<HelpURLAttribute>()
                    .ToDictionary(type => type, type => type.GetCustomAttribute<HelpURLAttribute>().URL)
                    .Where(pair => pair.Value.Contains("//docs.coherence.io/"));

                var componentUrls = documentedTypes.Select(pair => $"{pair.Key}\n<a href=\"{pair.Value}\">{pair.Value}</a>");

                Debug.Log("GitBook URLs\n" + string.Join("\n", urls) + "\n\nComponent URLs\n" + string.Join("\n", componentUrls));
            }

            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        private void DrawMiscSettings()
        {
            // defaultAuthorityTransferType is unfortunately an int because the Coherence assembly cannot reference the Toolkit assembly
            // so we need to cast it to the enum and render it manually with EnumPopup.
            var defaultAuthorityTransferTypeValue = (CoherenceSync.AuthorityTransferType)defaultAuthorityTransferType.intValue;
            defaultAuthorityTransferTypeValue = (CoherenceSync.AuthorityTransferType)EditorGUILayout.EnumPopup(GUIContents.defaultAuthorityTransferType, defaultAuthorityTransferTypeValue);
            defaultAuthorityTransferType.intValue = (int)defaultAuthorityTransferTypeValue;

#if COHERENCE_ENABLE_MULTI_ROOM_SIMULATOR
            if (runtimeSettings)
            {
                _ = EditorGUILayout.PropertyField(localDevelopmentMode, GUIContents.localDevelopmentMode);
            }
#endif

            _ = EditorGUILayout.PropertyField(showHubModuleQuickHelp, GUIContents.showHubQuickHelp);
            _ = EditorGUILayout.PropertyField(reportAnalytics, GUIContents.reportAnalytics);
        }

        private void DrawScriptingDefine(string define)
        {
            const int iconWidth = 16;
            const int horizontalSpace = 2;
            const int controlHeight = 16;

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var defines);
            var hasDefine = Array.IndexOf(defines, define) != -1;

            var labelStyle = ContentUtils.GUIStyles.miniLabel;
            var rect = EditorGUILayout.GetControlRect(false, controlHeight, labelStyle);
            rect = EditorGUI.IndentedRect(rect);
            var content = EditorGUIUtility.IconContent(hasDefine ? "Toolbar Minus" : "Toolbar Plus");
            var iconStyle = ContentUtils.GUIStyles.iconButton;

            var iconRect = rect;
            iconRect.width = iconWidth;

            if (GUI.Button(iconRect, content, iconStyle))
            {
                if (hasDefine)
                {
                    ArrayUtility.Remove(ref defines, define);
                }
                else
                {
                    ArrayUtility.Add(ref defines, define);
                }

                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
            }

            var labelRect = rect;
            labelRect.xMin += iconRect.width + horizontalSpace;

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.SelectableLabel(labelRect, define, labelStyle);
            EditorGUI.indentLevel = indent;
        }

        private void DrawLogs()
        {
            if (!runtimeSettings)
            {
                return;
            }

            EditorGUILayout.LabelField(GUIContents.logsTitle, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            logSettings.EditorLogLevel = (LogLevel)EditorGUILayout.EnumPopup(GUIContents.editorLogLevel, logSettings.EditorLogLevel);
            logSettings.LogLevel = (LogLevel)EditorGUILayout.EnumPopup(GUIContents.consoleLogLevel, logSettings.LogLevel);
            DefinesManager.ApplyCorrectLogLevelDefines(logSettings.LogLevel);
            EditorGUI.indentLevel++;
            _ = EditorGUILayout.BeginHorizontal();
            logSettings.SourceFilters = EditorGUILayout.TextField(GUIContents.consoleLogFilter, logSettings.SourceFilters);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            // enum props are always rendered honoring indent, but here we want to have an "inline" element in horizontal space
            logSettings.FilterMode = (Log.FilterMode)EditorGUILayout.EnumPopup(logSettings.FilterMode, GUILayout.Width(68));
            EditorGUI.indentLevel = indent;
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;

            logSettings.LogStackTrace = EditorGUILayout.Toggle(GUIContents.logStackTrace, logSettings.LogStackTrace);

            if (EditorGUI.EndChangeCheck())
            {
                logSettings.Save();
            }

            logSettings.LogToFile = EditorGUILayout.Toggle(GUIContents.logToFile, logSettings.LogToFile);
            EditorGUI.BeginDisabledGroup(!logSettings.LogToFile);
            EditorGUI.indentLevel++;
            logSettings.LogFilePath = EditorGUILayout.TextField(GUIContents.logFilePath, logSettings.LogFilePath);
            logSettings.FileLogLevel = (LogLevel)EditorGUILayout.EnumPopup(GUIContents.logLevel, logSettings.FileLogLevel);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.LabelField("Scripting Defines", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawScriptingDefine(LogConditionals.DisableInfo);
            DrawScriptingDefine(LogConditionals.DisableWarning);
            DrawScriptingDefine(LogConditionals.DisableError);
            EditorGUILayout.LabelField(GUIContents.scriptingDefineHelp, ContentUtils.GUIStyles.miniLabelGreyWrap);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }

        private void DrawRsLogs()
        {
            EditorGUILayout.LabelField(GUIContents.rsLogsTitle, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(rsConsoleLogLevel, GUIContents.rsConsoleLogLevel);
            EditorGUILayout.PropertyField(rsLogToFile, GUIContents.rsLogToFile);
            EditorGUI.BeginDisabledGroup(!rsLogToFile.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(rsLogFilePath, GUIContents.logFilePath);
            EditorGUILayout.PropertyField(rsFileLogLevel, GUIContents.logLevel);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }


        private void DrawLocalReplicationServer()
        {
            EditorGUILayout.LabelField(GUIContents.replicationServerTitle, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
            _ = EditorGUILayout.PropertyField(bundleRs, GUIContents.bundleReplicationServer);
            _ = EditorGUILayout.PropertyField(keepConnectionAlive, GUIContents.keepConnectionAliveLabel);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            _ = EditorGUILayout.BeginHorizontal();
            var r = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.frameBox);

            var modeCount = 2;
            for (int i = 0; i < modeCount; i++)
            {
                var tabRect = ContentUtils.GetTabRect(r, i, modeCount, out var tabStyle);
                EditorGUI.BeginChangeCheck();
                var m = GUI.Toggle(tabRect, (int)mode == i, ((Mode)i).ToString(), tabStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    GUI.FocusControl(null);
                    if (m)
                    {
                        mode = (Mode)i;
                        SessionState.SetInt(modeSessionKey, i);
                    }
                }
            }

            _ = GUILayoutUtility.GetRect(10, 22);

            // inner frame contents

            if (mode == Mode.Rooms)
            {
                EditorGUI.BeginChangeCheck();
                _ = EditorGUILayout.PropertyField(roomsUDPPort, GUIContents.port);
                _ = EditorGUILayout.PropertyField(roomsWebPort, GUIContents.webPort);
                _ = EditorGUILayout.PropertyField(sendFrequency);
                _ = EditorGUILayout.PropertyField(recvFrequency);
                _ = EditorGUILayout.PropertyField(localRoomsCleanupTimeSeconds);
                if (EditorGUI.EndChangeCheck())
                {
                    roomsUDPPort.intValue = roomsUDPPort.intValue < 0 ? 0 : roomsUDPPort.intValue;
                    if (runtimeSettings)
                    {
                        roomsWebPort.intValue = roomsWebPort.intValue < 0 ? 0 : roomsWebPort.intValue;
                    }

                    sendFrequency.intValue = sendFrequency.intValue < 1 ? 1 : sendFrequency.intValue;
                    recvFrequency.intValue = recvFrequency.intValue < 1 ? 1 : recvFrequency.intValue;
                }

                GUILayout.Label(GUIContents.localRoomHostAuthorityHelp, ContentUtils.GUIStyles.miniLabelGreyWrap);
            }
            else if (mode == Mode.Worlds)
            {
                EditorGUI.BeginChangeCheck();
                _ = EditorGUILayout.PropertyField(worldUDPPort, GUIContents.port);
                _ = EditorGUILayout.PropertyField(worldWebPort, GUIContents.webPort);
                _ = EditorGUILayout.PropertyField(sendFrequency);
                _ = EditorGUILayout.PropertyField(recvFrequency);
                if (EditorGUI.EndChangeCheck())
                {
                    worldUDPPort.intValue = worldUDPPort.intValue < 0 ? 0 : worldUDPPort.intValue;
                    worldWebPort.intValue = worldWebPort.intValue < 0 ? 0 : worldWebPort.intValue;
                    sendFrequency.intValue = sendFrequency.intValue < 1 ? 1 : sendFrequency.intValue;
                    recvFrequency.intValue = recvFrequency.intValue < 1 ? 1 : recvFrequency.intValue;
                }

                _ = EditorGUILayout.PropertyField(localWorldHostAuthority, GUIContents.localWorldHostAuthority);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawMissingRuntimeSettings()
        {
            if (!runtimeSettings)
            {
                if (GUILayout.Button("Initialize Runtime Settings", EditorStyles.miniButton))
                {
                    Postprocessor.UpdateRuntimeSettings();
                    Refresh();
                }
            }
        }

        private static void ShowNotification(string notification)
        {
            if (!EditorWindow.focusedWindow)
            {
                Debug.Log(notification);
                return;
            }

            EditorWindow.focusedWindow.ShowNotification(new GUIContent(notification), 1f);
        }
    }
}
