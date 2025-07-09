// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

#pragma warning disable CS0618 // Type or member is obsolete
    [CustomEditor(typeof(Coherence.Toolkit.ConnectionEventHandler)), CanEditMultipleObjects]
#pragma warning restore CS0618 // Type or member is obsolete
    internal class ConnectionEventHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CoherenceHeader.OnSlimHeader(string.Empty);
            EditorGUILayout.HelpBox("ConnectionEventHandle is deprecated and will be completely removed in the future.", MessageType.Warning);

            EditorGUILayout.LabelField("Fires events on connections and disconnections.", ContentUtils.GUIStyles.miniLabelGreyWrap);

            EditorGUILayout.Space();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
