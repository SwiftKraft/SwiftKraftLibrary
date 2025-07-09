// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

#pragma warning disable CS0618 // Type or member is obsolete
    [CustomEditor(typeof(Simulator.MultiRoomSimulator)), CanEditMultipleObjects]
#pragma warning restore CS0618 // Type or member is obsolete
    internal class MultiRoomSimulatorEditor : BaseEditor
    {
#pragma warning disable CS0618 // Type or member is obsolete
        protected override string Description => $"Enables Multi-Room Simulator support by listening to request @ PUT localhost:{ProjectSettings.instance.RuntimeSettings.LocalHttpServerPort}/rooms. Requests trigger scene loading via {nameof(Coherence.Toolkit.CoherenceSceneLoader)}. For local development, use {nameof(Simulator.MultiRoomSimulatorLocalForwarder)} to send requests.";
#pragma warning restore CS0618 // Type or member is obsolete

        protected override void OnGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Multi-Room Simulators are deprecated and will be completely removed in the future.", MessageType.Warning);
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
