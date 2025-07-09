// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;
    using System.Linq;
    using System.Collections.Generic;

#if COHERENCE_USE_TAG_DROPDOWN
    [CustomPropertyDrawer(typeof(CoherenceTagAttribute))]
#endif
    internal class CoherenceTagDrawer : PropertyDrawer
    {
        private int popUpWidth = 20;
        private class GUIContents
        {
            public static readonly GUIContent tag = EditorGUIUtility.TrTextContent("CoherenceTag", $"Adding a `tag` makes this object visible to any clients with a {nameof(CoherenceTagQuery)} using the corresponding tag.");
            public static readonly GUIContent popUp = EditorGUIUtility.TrTextContent("", $"List of current tags in {nameof(CoherenceSync)} components");
        }

        private int selectionIndex = -1;
        private string[] options;

        public CoherenceTagDrawer() : base()
        {
            var prefabs = new List<CoherenceSync>();
            CoherenceSyncConfigRegistry.Instance.GetReferencedPrefabs(prefabs);
            options = prefabs
                .Select(sync => sync.CoherenceTag)
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (selectionIndex == -1 && options.Contains(property.stringValue))
            {
                selectionIndex = System.Array.IndexOf(options, property.stringValue);
            }

            bool drawPopup = options.Length > 0;

            //Make room for the popup
            if (drawPopup)
            {
                position.width -= popUpWidth;
            }

            EditorGUI.PropertyField(position, property, GUIContents.tag);

            if (!drawPopup)
            {
                return;
            }

            //Draw the popup to select tags
            var btnPos = new Rect((position.x + position.width), position.y, popUpWidth, position.height);
            EditorGUI.BeginChangeCheck();
            selectionIndex = EditorGUI.Popup(
            btnPos,
            "",
            selectionIndex,
            options);

            //Tooltip on top of the popup button because I dont want to show dropdown label
            EditorGUI.LabelField(btnPos, GUIContents.popUp);

            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = options[selectionIndex];
            }
        }
    }
}
