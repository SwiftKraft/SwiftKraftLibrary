// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System;
    using Interpolation;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(Interpolator))]
    internal class InterpolatorDrawer : PropertyDrawer
    {
        private TypeCache.TypeCollection cachedTypes = TypeCache.GetTypesDerivedFrom(typeof(Interpolator));

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var options = new GUIContent[cachedTypes.Count + 1];
            options[0] = new GUIContent(InterpolationSettings.EmptyInterpolationName);

            var selectedIndex = 0;
            for (var i = 0; i < cachedTypes.Count; i++)
            {
                var type = cachedTypes[i];
                options[i + 1] = new GUIContent(ObjectNames.NicifyVariableName(type.Name));
                var typeAndAssemblyName = $"{type.Assembly.GetName().Name} {type.FullName}";
                if (property.managedReferenceFullTypename == typeAndAssemblyName)
                {
                    selectedIndex = i + 1;
                }
            }

            var newSelectedIndex = EditorGUILayout.Popup(label, selectedIndex, options);
            if (selectedIndex != newSelectedIndex)
            {
                Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, "selected type change");
                if (newSelectedIndex == 0)
                {
                    property.managedReferenceValue = new Interpolator();
                }
                else
                {
                    var selectedType = cachedTypes[newSelectedIndex - 1];
                    property.managedReferenceValue = Activator.CreateInstance(selectedType);
                }
            }

            if (property.hasVisibleChildren)
            {
                ContentUtils.DrawSection(property, EditorGUIUtility.TrTempContent("Interpolation Parameters"));
            }
        }
    }
}
