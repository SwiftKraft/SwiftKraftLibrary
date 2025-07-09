// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(CoherenceSyncConfigPickerAttribute))]
    internal class CoherenceSyncConfigPickerDrawer : ObjectPickerDrawer
    {
        private bool invalidObjectChosen;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var syncConfig = property.objectReferenceValue as CoherenceSyncConfig;
            var sync = syncConfig ? syncConfig.Sync : null;
            invalidObjectChosen = false;

            EditorGUI.BeginProperty(rect, label, property);
            EditorGUI.BeginChangeCheck();
            sync = ComponentObjectField(rect, label, sync);
            if (EditorGUI.EndChangeCheck())
            {
                if (invalidObjectChosen)
                {
                    EditorUtility.DisplayDialog("Missing Component", $"The selected object must have a {nameof(CoherenceSync)} component.", "OK");
                }
            }
            EditorGUI.EndProperty();

            if (!sync)
            {
                property.objectReferenceValue = null;
                return;
            }

            property.objectReferenceValue = sync.CoherenceSyncConfig ? sync.CoherenceSyncConfig : null;
        }

        private T ComponentObjectField<T>(Rect position, GUIContent label, T obj) where T : Component
        {
            var newObj = (GameObject)EditorGUI.ObjectField(position, label, obj ? obj.gameObject : null,
                typeof(GameObject), false);

            if (!newObj)
            {
                return null;
            }

            if (newObj.TryGetComponent<T>(out var component))
            {
                return component;
            }

            invalidObjectChosen = true;
            return obj;
        }
    }
}
