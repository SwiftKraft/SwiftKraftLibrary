// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using Toolkit;

    internal class GameBuildWindow : EditorWindow
    {
        private static string buildPath;
        private static AvailablePlatforms _platformSelected = 0;

        private static bool validBuildPath;

        private static string ProjectId =>
            $"{PlayerSettings.companyName}.{PlayerSettings.productName}";

        private static string BuildPathPrefix =>
            $"Coherence.{ProjectId}.Build.Path";

        private static string BuildPlatformKey =>
            $"Coherence.{ProjectId}.Build.Platform";

        private class GUIContents
        {
            public static readonly GUIContent title = Icons.GetContentWithText("EditorWindow", "Build Upload");
            public static readonly GUIContent uploadButton = EditorGUIUtility.TrTextContent("Upload");

            public static readonly GUIContent[] toolbarButtons = new GUIContent[]
            {
                uploadButton,
            };
        }

        private int toolbarSelected;

        private System.Action[] toolbarFns;

        private readonly static Dictionary<AvailablePlatforms, BuildPathValidator> buildPathValidators = new Dictionary<AvailablePlatforms, BuildPathValidator>()
        {
            { AvailablePlatforms.Linux, new LinuxPathValidator() },
            { AvailablePlatforms.macOS, new MacOSPathValidator() },
            { AvailablePlatforms.Windows, new WindowsPathValidator() },
            { AvailablePlatforms.WebGL, new WebGLPathValidator() }
        };

        private readonly static Dictionary<AvailablePlatforms, BuildUploader> buildUploaders = new Dictionary<AvailablePlatforms, BuildUploader>()
        {
            { AvailablePlatforms.Linux, new DefaultUploader() },
            { AvailablePlatforms.macOS, new MacOSUploader() },
            { AvailablePlatforms.Windows, new DefaultUploader() },
            { AvailablePlatforms.WebGL, new WebGLUploader() }
        };

        private GameBuildWindow()
        {
            toolbarFns = new System.Action[]
            {
                OnUploadGUI,
            };
        }

        internal static void RestoreSavedBuildSettings()
        {
            _platformSelected = LoadSelectedBuildPlatform();
            buildPath = GetBuildPathForSelectedPlatform();
            UpdateBuildPathValidity();
        }

        private Vector2 infoScroll;

        internal static void DrawShareBuildGUI()
        {
            var uploadType = _platformSelected == AvailablePlatforms.macOS ? "'.app'" : "folder";

            CoherenceHubLayout.DrawInfoLabel($"The selected {uploadType} will be compressed and uploaded to the coherence Online Server.\nDepending on the size of the build, this step might take a few minutes to complete.");

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                _platformSelected = (AvailablePlatforms)EditorGUILayout.Popup("Platform", (int)_platformSelected, Enum.GetNames(typeof(AvailablePlatforms)));

                if (change.changed)
                {
                    SaveSelectedBuildPlatform(_platformSelected);
                    buildPath = GetBuildPathForSelectedPlatform();
                }
            }

            CoherenceHubLayout.DrawDiskPath(buildPath, "Select path...", GetBuildSelector(), (newPath) =>
            {
                buildPath = newPath;
                SetBuildPathForSelectedPlatform(buildPath);
                GUIUtility.ExitGUI();
            });

            if (!buildPathValidators[_platformSelected].Validate(buildPath))
            {
                var infoString = buildPathValidators[_platformSelected].GetInfoString();
                CoherenceHubLayout.DrawMessageArea(infoString);
            }

            UpdateBuildPathValidity();
            var tooltip = string.Empty;

            if (!validBuildPath)
            {
                tooltip = "Build path is invalid.";
            }
            else if (!PortalUtil.CanCommunicateWithPortal)
            {
                tooltip = "You need to login to upload builds.";
            }

            if (!PortalUtil.OrgAndProjectIsSet)
            {
                tooltip = "Organization and project must be set to upload simulator builds to the cloud.";
            }

            CoherenceHubLayout.DrawCloudDependantButton(new GUIContent("Upload Game Build to Cloud"), () =>
            {
                if (!validBuildPath)
                {
                    Debug.LogError("Build path is invalid.");
                    buildPath = string.Empty;
                    return;
                }

                var uploader = buildUploaders[_platformSelected];

                if (uploader.AllowUpload(buildPath))
                {
                    uploader.Upload(_platformSelected, buildPath);
                    GUIUtility.ExitGUI();
                }
            }, tooltip, DisableConditions, ContentUtils.GUIStyles.bigButton);
        }

        private static string BuildPathKeyForSelectedPlatform =>
            $"{BuildPathPrefix}.{_platformSelected}";

        private static string GetBuildPathForSelectedPlatform()
        {
            return EditorPrefs.GetString(BuildPathKeyForSelectedPlatform, string.Empty);
        }

        private static void SetBuildPathForSelectedPlatform(string value)
        {
            EditorPrefs.SetString(BuildPathKeyForSelectedPlatform, value);
        }

        private static AvailablePlatforms LoadSelectedBuildPlatform()
        {
            var defaultPlatform = SystemInfo.operatingSystemFamily switch
            {
                OperatingSystemFamily.MacOSX => AvailablePlatforms.macOS,
                OperatingSystemFamily.Linux => AvailablePlatforms.Linux,
                _ => AvailablePlatforms.Windows
            };

            return (AvailablePlatforms)EditorPrefs.GetInt(BuildPlatformKey, (int)defaultPlatform);
        }

        private static void SaveSelectedBuildPlatform(AvailablePlatforms platform)
        {
            EditorPrefs.SetInt(BuildPlatformKey, (int)platform);
        }

        private static Func<string> GetBuildSelector()
        {
#if UNITY_EDITOR_OSX
            return _platformSelected == AvailablePlatforms.macOS ? (Func<string>)OpenFilePanel : OpenFolderPanel;
#else
            return OpenFolderPanel;
#endif
        }

        private static bool DisableConditions() => !PortalUtil.OrgAndProjectIsSet || !validBuildPath;

        private static string OpenFolderPanel()
        {
            return EditorUtility.OpenFolderPanel("Select a game build folder", "Builds", string.Empty);
        }

        private static string OpenFilePanel()
        {
            return EditorUtility.OpenFilePanel("Select a macOS app", "Builds", ".app");
        }


        internal static void Init()
        {
            _ = GetWindow<GameBuildWindow>();
        }

        private void OnEnable()
        {
            titleContent = GUIContents.title;
        }

        private void OnFocus()
        {
            UpdateBuildPathValidity();
        }

        private void OnUploadGUI()
        {
            CoherenceHeader.OnSlimHeader(string.Empty);

            DrawShareBuildGUI();
        }

        private void OnGUI()
        {
            _ = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            GUILayout.Space(4);
            toolbarSelected = GUILayout.Toolbar(toolbarSelected, GUIContents.toolbarButtons, EditorStyles.toolbarButton, GUI.ToolbarButtonSize.FitToContents);
            if (EditorGUI.EndChangeCheck())
            {
            }
            EditorGUILayout.EndHorizontal();

            toolbarFns[toolbarSelected]();
        }

        private static void UpdateBuildPathValidity()
        {
            validBuildPath = buildPathValidators[_platformSelected].Validate(buildPath);
        }
    }
}
