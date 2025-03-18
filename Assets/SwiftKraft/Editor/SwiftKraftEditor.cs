using System.Linq;
using UnityEditor;

namespace SwiftKraft.Editors
{
    public class SwiftKraftEditor : Editor
    {
        public override void OnInspectorGUI() => serializedObject.UpdateIfRequiredOrScript();

        public void DrawDefaultInspectorExcluding(params string[] properties)
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    if (!properties.Contains(iterator.name))
                        EditorGUILayout.PropertyField(iterator, true);
                }

                enterChildren = false;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
