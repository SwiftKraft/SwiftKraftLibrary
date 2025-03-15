using SwiftKraft.Editors;
using UnityEditor;

namespace SwiftKraft.Gameplay.Weapons.Editors
{
    [CustomEditor(typeof(WeaponSpread), true)]
    [CanEditMultipleObjects]
    public class WeaponSpreadEditor : SwiftKraftEditor
    {
        SerializedProperty spreadTransform;
        SerializedProperty spread;
        SerializedProperty spread_aim;
        SerializedProperty spread_recoil;
        SerializedProperty spread_aim_recoil;

        SerializedProperty recoil;
        SerializedProperty aim;

        private void OnEnable()
        {
            spreadTransform = serializedObject.FindProperty(nameof(WeaponSpread.SpreadTransform));
            spread = serializedObject.FindProperty(nameof(WeaponSpread.Spread));
            spread_aim = serializedObject.FindProperty(nameof(WeaponSpread.SpreadAim));
            spread_recoil = serializedObject.FindProperty(nameof(WeaponSpread.SpreadRecoil));
            spread_aim_recoil = serializedObject.FindProperty(nameof(WeaponSpread.SpreadAimRecoil));

            recoil = serializedObject.FindProperty(nameof(WeaponSpread.Recoil));
            aim = serializedObject.FindProperty(nameof(WeaponSpread.Aim));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawDefaultInspectorExcluding(nameof(WeaponSpread.SpreadTransform), nameof(WeaponSpread.Spread), nameof(WeaponSpread.SpreadAim), nameof(WeaponSpread.SpreadRecoil), nameof(WeaponSpread.SpreadAimRecoil), nameof(WeaponSpread.Recoil), nameof(WeaponSpread.Aim));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(spreadTransform);
            EditorGUILayout.PropertyField(recoil);
            EditorGUILayout.PropertyField(aim);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Values", EditorStyles.boldLabel);

            if (aim.objectReferenceValue != null && recoil.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(spread_recoil);
                EditorGUILayout.PropertyField(spread_aim_recoil);
            }
            else if (aim.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(spread);
                EditorGUILayout.PropertyField(spread_aim);
            }
            else if (recoil.objectReferenceValue != null)
                EditorGUILayout.PropertyField(spread_recoil);
            else
                EditorGUILayout.PropertyField(spread);

            serializedObject.ApplyModifiedProperties();
        }
    }
}