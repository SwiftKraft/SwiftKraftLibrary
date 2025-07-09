// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

#pragma warning disable CS0618 // Type or member is obsolete
    [CustomEditor(typeof(Simulator.SimulatorEventHandler)), CanEditMultipleObjects]
#pragma warning restore CS0618 // Type or member is obsolete
    internal class SimulatorEventHandlerEditor : Editor
    {
        private SerializedProperty editorIsSimulator;

        private SerializedProperty onSimulatorAwake;
        private SerializedProperty onSimulatorConnect;

        private SerializedProperty onClientAwake;
        private SerializedProperty onClientConnect;

        private void OnEnable()
        {
            editorIsSimulator = serializedObject.FindProperty("editorIsSimulator");

            onSimulatorAwake = serializedObject.FindProperty("onSimulatorAwake");
            onSimulatorConnect = serializedObject.FindProperty("onSimulatorConnect");
            onClientAwake = serializedObject.FindProperty("onClientAwake");
            onClientConnect = serializedObject.FindProperty("onClientConnect");
        }

        private void OnDisable()
        {
            editorIsSimulator.Dispose();
            onSimulatorAwake.Dispose();
            onSimulatorConnect.Dispose();
            onClientAwake.Dispose();
            onClientConnect.Dispose();
        }

        public override void OnInspectorGUI()
        {
            CoherenceHeader.OnSlimHeader(string.Empty);
            EditorGUILayout.HelpBox("SimulatorEventHandler is deprecated and will be completely removed in the future.", MessageType.Warning);

            EditorGUILayout.LabelField("Exposes callbacks to tell apart if it should run as a simulator or not.", ContentUtils.GUIStyles.miniLabelGreyWrap);

            EditorGUILayout.Space();

            serializedObject.Update();

            _ = EditorGUILayout.PropertyField(editorIsSimulator);

            onClientAwake.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(onClientAwake.isExpanded, "Client Events");
            if (onClientAwake.isExpanded)
            {
                _ = EditorGUILayout.PropertyField(onClientAwake);
                _ = EditorGUILayout.PropertyField(onClientConnect);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            onSimulatorAwake.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(onSimulatorAwake.isExpanded, "Simulator Events");
            if (onSimulatorAwake.isExpanded)
            {
                _ = EditorGUILayout.PropertyField(onSimulatorAwake);
                _ = EditorGUILayout.PropertyField(onSimulatorConnect);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
