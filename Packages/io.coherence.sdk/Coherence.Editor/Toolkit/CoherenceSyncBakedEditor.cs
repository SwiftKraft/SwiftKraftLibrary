// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;
    using Coherence.Toolkit;

    [CustomEditor(typeof(CoherenceSyncBaked), true)]
    internal class CoherenceSyncBakedEditor : BaseEditor
    {
        protected override void OnGUI()
        {
            var c = EditorGUIUtility.TrTempContent("Baked script valid for " + target.GetType().Name.Substring(13));
            EditorGUILayout.LabelField(c, EditorStyles.centeredGreyMiniLabel);
            c = EditorGUIUtility.TrTempContent("You're all setup! This prefab uses baked mode.");
            EditorGUILayout.LabelField(c, EditorStyles.centeredGreyMiniLabel);
        }
    }
}
