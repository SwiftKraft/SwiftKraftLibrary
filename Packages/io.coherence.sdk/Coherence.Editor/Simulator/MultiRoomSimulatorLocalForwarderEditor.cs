// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using Coherence.Toolkit;
    using Simulator;
    using UnityEditor;
    using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
    [CustomEditor(typeof(MultiRoomSimulatorLocalForwarder)), CanEditMultipleObjects]
#pragma warning restore CS0618 // Type or member is obsolete
    internal class MultiRoomSimulatorLocalForwarderEditor : BaseEditor
    {
        protected override string Description
        {
            get
            {
                var rs = ProjectSettings.instance.RuntimeSettings;
                return
                    $"When the CoherenceBridge connects, sends request to PUT {rs.LocalHttpServerHost}:{rs.LocalHttpServerPort}/rooms. Allows local development for multi-room simulators. To listen to the requests use MultiRoomSimulator.";
            }
        }

        private static class GUIContents
        {
            public static readonly GUIContent connected =
                EditorGUIUtility.TrTextContentWithIcon("Connected", Icons.GetPath("Coherence.Connected"));

            public static readonly GUIContent disconnected =
                EditorGUIUtility.TrTextContentWithIcon("Disconnected", Icons.GetPath("Coherence.Disconnected"));

            public static readonly GUIContent noBridge =
                EditorGUIUtility.TrTextContentWithIcon("No CoherenceBridge associated!", "Warning");
        }

        private SerializedProperty localDevelopmentMode;

        protected override void OnEnable()
        {
            base.OnEnable();
            var rs = ProjectSettings.instance.RuntimeSettings;
            if (rs)
            {
                var rsso = new SerializedObject(ProjectSettings.instance.RuntimeSettings);
                localDevelopmentMode = rsso.FindProperty("localDevelopmentMode");
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (localDevelopmentMode != null)
            {
                localDevelopmentMode.Dispose();
            }
        }

        protected override void OnGUI()
        {
            EditorGUILayout.HelpBox("MultiRoomSimulatorLocalForwarder is deprecated and will be completely removed in the future.", MessageType.Warning);

            if (localDevelopmentMode != null && !localDevelopmentMode.boolValue)
            {
                localDevelopmentMode.serializedObject.Update();
                _ = EditorGUILayout.PropertyField(localDevelopmentMode);
                EditorGUILayout.LabelField(
                    "This setting is part of RuntimeSettings, and can be accessed from the settings window.",
                    ContentUtils.GUIStyles.miniLabelGreyWrap);
                _ = localDevelopmentMode.serializedObject.ApplyModifiedProperties();

                EditorGUILayout.Space();
            }

            if (Application.isPlaying && targets.Length == 1)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var mrslf = target as MultiRoomSimulatorLocalForwarder;
#pragma warning restore CS0618 // Type or member is obsolete
                if (mrslf)
                {
                    if (CoherenceBridgeStore.TryGetBridge(mrslf.gameObject.scene, out var bridge))
                    {
                        EditorGUILayout.LabelField(bridge.IsConnected
                            ? GUIContents.connected
                            : GUIContents.disconnected);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(GUIContents.noBridge);
                    }
                }
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
