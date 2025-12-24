using SwiftKraft.Editors;
using UnityEditor;
using UnityEngine;

namespace SwiftKraft.Utils.Editors
{
    [CustomEditor(typeof(RigDefinition))]
    public class RigDefinitionEditor : SwiftKraftEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspectorExcluding(nameof(RigDefinition.RootNode));

            RigDefinition rig = (RigDefinition)target;

            GUILayout.Space(8);

            if (GUILayout.Button("Rebuild Transforms From Root"))
            {
                Undo.RecordObject(rig, "Rebuild Rig Transforms");
                rig.Rebuild();
                EditorUtility.SetDirty(rig);
            }
        }
    }
}