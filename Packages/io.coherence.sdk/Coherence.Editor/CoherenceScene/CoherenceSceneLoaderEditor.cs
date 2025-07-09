// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

    [CustomEditor(typeof(Coherence.Toolkit.CoherenceSceneLoader)), CanEditMultipleObjects]
    internal class CoherenceSceneLoaderEditor : BaseEditor
    {
        protected override string Description => "Loads scenes additively";

        protected override void OnGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
