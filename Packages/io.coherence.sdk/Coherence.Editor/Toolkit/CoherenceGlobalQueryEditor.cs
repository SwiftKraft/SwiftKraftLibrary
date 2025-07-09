// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Coherence.Toolkit.CoherenceGlobalQuery))]
    internal class CoherenceGlobalQueryEditor : BaseEditor
    {
        protected override void OnGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
