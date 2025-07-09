// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using Coherence.Toolkit.Archetypes;
    using UnityEditor.IMGUI.Controls;

    internal class BindingsTreeViewHiddenRowsItem : BindingsTreeViewItem
    {
        public int HiddenItems => hiddenRows.Count;
        public BindingsTreeViewComponentItem BelongsTo { get; }

        private ArchetypeComponent boundComponent;
        private List<BindingsTreeViewBindingItem> hiddenRows = new List<BindingsTreeViewBindingItem>();

        internal BindingsTreeViewHiddenRowsItem(int id, TreeViewItem assingTo, ArchetypeComponent boundComponent, BindingsTreeViewComponentItem belongsTo)
        {
            this.boundComponent = boundComponent;
            belongsTo.HiddenField = this;
            assingTo.AddChild(this);

            Reset();
            Setup(rowHeight: 24, lodSteps: boundComponent.MaxLods);

            displayName = "Hidden";
            depth = 1;
            this.id = id;
        }

        protected override void DrawLeftBar(Rect rect)
        {
            GUIStyle miniText = new GUIStyle(EditorStyles.miniLabel);
            miniText.alignment = TextAnchor.MiddleLeft;

            Rect nameRect = new Rect(rect.x + 12, rect.y, rect.width - 12, rect.height);
            GUI.Label(nameRect, $"{hiddenRows.Count} hidden {(hiddenRows.Count == 1 ? "field" : "fields")}", miniText);
        }

        protected override void DrawLOD(Rect rect, int step)
        {
            bool enabledOnComponent = boundComponent.LodStepsActive > step;
            if (enabledOnComponent)
            {
                int bits = 0;
                foreach (var hiddenRow in hiddenRows)
                {
                    bits += hiddenRow.GetBitsOfLOD(step);
                }

                GUIStyle miniText = enabledOnComponent ? new GUIStyle(EditorStyles.miniLabel) : new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                miniText.alignment = TextAnchor.MiddleRight;
                GUI.Label(rect, $"+ {bits} {(bits == 1 ? "Bit" : "Bits")}", miniText);
            }
        }

        internal void Reset()
        {
            hiddenRows.Clear();
        }

        internal void Add(BindingsTreeViewBindingItem filteredFieldItem)
        {
            hiddenRows.Add(filteredFieldItem);
        }
    }
}
