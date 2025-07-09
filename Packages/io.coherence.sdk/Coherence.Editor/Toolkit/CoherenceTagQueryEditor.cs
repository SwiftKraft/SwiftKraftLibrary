// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Coherence.Toolkit.CoherenceTagQuery))]
    internal class CoherenceTagQueryEditor : BaseEditor
    {
        protected override void OnGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
