// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Build;
    using UnityEditor;
    using UnityEngine;
    using System;
    using Connection;

    internal class SimulatorWindow : EditorWindow
    {
        private SimulatorBuildOptions buildOptions;
        private EndpointData endpoint;
        private string executablePath;
        private Vector2 scrollPosition;

        private class GUIContents
        {
            public static readonly GUIContent title = Icons.GetContentWithText("EditorWindow", "Simulator");
            public static readonly GUIContent infoButton = EditorGUIUtility.TrTextContent("Info");
            public static readonly GUIContent buildButton = EditorGUIUtility.TrTextContent("Build");
            public static readonly GUIContent testButton = EditorGUIUtility.TrTextContent("Test");

            public static readonly GUIContent[] toolbarButtons = new GUIContent[]
            {
                infoButton,
                buildButton,
                testButton
            };
        }

        private int toolbarSelected;

        private Action[] toolbarFns;

        private SimulatorWindow()
        {
            toolbarFns = new Action[]
            {
                OnInfoGUI,
                OnBuildGUI,
                OnTestGUI,
            };
        }

        private Vector2 infoScroll;
        private bool[] infoFoldouts = new bool[4];

        internal static void Init()
        {
            _ = GetWindow<SimulatorWindow>();
        }

        private void OnEnable()
        {
            minSize = new Vector2(300, 250);
            titleContent = GUIContents.title;

            endpoint = SimulatorEditorUtility.LastUsedEndpoint;

            buildOptions = SimulatorBuildOptions.Get();
        }

        private void OnInfoGUI()
        {
            CoherenceHeader.OnSlimHeader(string.Empty);

            infoScroll = EditorGUILayout.BeginScrollView(infoScroll);

            EditorGUILayout.LabelField("To simulate part of your game world server-side, you will need to build a simulator.\nSimulators are headless game clients without graphics that run specialized game logic.\nYou can differentiate between code running on the client vs. a simulator using preprocessor definitions and command line arguments.\ncoherence supports any number of simulators running in the cloud and simulating different areas or aspects of the game world.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();

            infoFoldouts[0] = EditorGUILayout.BeginFoldoutHeaderGroup(infoFoldouts[0], "What is a simulator?");
            if (infoFoldouts[0])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Simulators are game clients with custom logic that run behaviour different to user-controlled game clients.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("You can host your simulators at coherence.", EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            infoFoldouts[1] = EditorGUILayout.BeginFoldoutHeaderGroup(infoFoldouts[1], "Shaping simulator behaviour");

            if (infoFoldouts[1])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Use the COHERENCE_SIMULATOR preprocessor definition to build custom simulator logic.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Make sure simulators handle auto-connection and reconnection.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("We will soon have built-in custom behaviors for simulators. For now, you will have to build your own abstractions.", EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            infoFoldouts[2] = EditorGUILayout.BeginFoldoutHeaderGroup(infoFoldouts[2], "How to build simulators?");

            if (infoFoldouts[2])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Use this tool in conjunction with a build configuration.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Create new build configurations from Assets/Create/coherence/Simulator Build Configuration.", EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            infoFoldouts[3] = EditorGUILayout.BeginFoldoutHeaderGroup(infoFoldouts[3], "Custom simulator build pipeline");
            if (infoFoldouts[3])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("You can build simulators with your own build pipeline instead.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Build them for Linux (64-bit).", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Build them with the COHERENCE_SIMULATOR player define symbol.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Build them with product name 'sim_x86_64'.", EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndScrollView();
        }

        private void OnBuildGUI()
        {
            CoherenceHeader.OnSlimHeader(string.Empty);

            using var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition);

            scrollPosition = scroll.scrollPosition;

            CoherenceHubLayout.DrawSectionHeader("Simulator Build Options");

            EditorGUI.indentLevel++;
            SimulatorGUI.DrawSimulatorBuildOptions(buildOptions, position.width);
            EditorGUI.indentLevel--;

            CoherenceHubLayout.DrawSectionHeader("Headless Linux Build");

            EditorGUI.indentLevel++;
            SimulatorGUI.DrawCreateAndUploadHeadlessSimulatorBuild(buildOptions);
            EditorGUI.indentLevel--;
            
            CoherenceHubLayout.DrawSectionHeader("AutoSimulatorConnection Component");

            EditorGUI.indentLevel++;
            SimulatorGUI.DrawAutoSimulatorConnection();
            EditorGUI.indentLevel--;

            CoherenceHubLayout.DrawSectionHeader("Local Simulator Build");

            EditorGUI.indentLevel++;
            SimulatorGUI.DrawLocalSimulatorBuild(buildOptions);
            EditorGUI.indentLevel--;
        }

        private void OnTestGUI()
        {
            CoherenceHeader.OnSlimHeader(string.Empty);

            SimulatorGUI.DrawRunSimulatorSettings(ref endpoint);
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

    }
}
