namespace Coherence.Editor.Toolkit
{
    using Simulator;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(AutoSimulatorConnection))]
    internal class AutoSimulatorConnectionEditor : BaseEditor
    {
        private SerializedProperty bridge;

        protected override void OnEnable()
        {
            base.OnEnable();
            bridge = serializedObject.FindProperty(AutoSimulatorConnection.Property.bridge);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            bridge.Dispose();
        }

    protected override void OnGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                AutoSimulatorConnection.Property.bridge);
            EditorGUI.BeginDisabledGroup(Application.isPlaying && !PrefabUtility.IsPartOfAnyPrefab(target));
            _ = EditorGUILayout.PropertyField(bridge);
            EditorGUI.EndDisabledGroup();
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
