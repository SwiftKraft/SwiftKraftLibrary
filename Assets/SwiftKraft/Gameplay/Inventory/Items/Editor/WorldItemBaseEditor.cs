using SwiftKraft.Editors;
using UnityEditor;

namespace SwiftKraft.Gameplay.Inventory.Items.Editors
{
    [CustomEditor(typeof(WorldItemBase), true)]
    public class WorldItemBaseEditor : SwiftKraftEditor
    {
        SerializedProperty startType;

        private void OnEnable()
        {
            startType = serializedObject.FindProperty(nameof(WorldItemBase.StartType));
        }

        public override void OnInspectorGUI()
        {
            WorldItemBase component = (WorldItemBase)target;

            bool isInScene = !PrefabUtility.IsPartOfPrefabAsset(component);

            DrawDefaultInspectorExcluding(nameof(WorldItemBase.StartType));

            if (isInScene)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Scene View Only", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(startType);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
