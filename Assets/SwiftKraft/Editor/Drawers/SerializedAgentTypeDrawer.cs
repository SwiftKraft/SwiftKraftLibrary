using SwiftKraft.Gameplay.NPCs;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomPropertyDrawer(typeof(SerializedAgentType))]
public class SerializedAgentTypeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var idProp = property.FindPropertyRelative("agentTypeID");
        int currentID = idProp.intValue;

        // Build agent list safely from public API
        int count = NavMesh.GetSettingsCount();
        string[] names = new string[count];
        int[] ids = new int[count];

        for (int i = 0; i < count; i++)
        {
            var settings = NavMesh.GetSettingsByIndex(i);
            ids[i] = settings.agentTypeID;
            names[i] = NavMesh.GetSettingsNameFromID(settings.agentTypeID);
        }

        // Find current selection index
        int currentIndex = System.Array.IndexOf(ids, currentID);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginProperty(position, label, property);
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, names);
        idProp.intValue = ids[newIndex];
        EditorGUI.EndProperty();
    }
}
