// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Archetypes;
    using UnityEditor.IMGUI.Controls;

    internal class BindingsTreeViewComponentItem : BindingsTreeViewItem
    {
        internal bool MissingScript { private set; get; }
        public BindingsTreeViewHiddenRowsItem HiddenField { get; set; }
        public bool EditingLocalBindings { get; set; }

        public BindingsTreeViewComponentGroup Fields { get; set; }
        public BindingsTreeViewComponentGroup Methods { get; set; }

        private ArchetypeComponent boundComponent;
        private BindingsWindowTree tree;
        private CoherenceSync sync;

        internal BindingsTreeViewComponentItem(int id, TreeViewItem assingTo, CoherenceSync sync, ArchetypeComponent boundComponent, BindingsWindowTree tree)
        {
            // TreeviewSpecific
            this.id = id;
            assingTo.AddChild(this);
            displayName = boundComponent.DisplayName.Substring(boundComponent.DisplayName.LastIndexOf(".") + 1); ;
            depth = 1;

            this.sync = sync;
            this.boundComponent = boundComponent;
            this.tree = tree;

            Fields = new BindingsTreeViewComponentGroup(this, sync, boundComponent, tree.BindingsWindow, false);
            Methods = new BindingsTreeViewComponentGroup(this, sync, boundComponent, tree.BindingsWindow, true);

            MissingScript = boundComponent.Component == null;

            Setup(rowHeight: 28, lodSteps: boundComponent.MaxLods);
        }

        protected override void DrawStatisticsBar(Rect rect)
        {
            if (boundComponent.HasSyncedBindings())
            {
                RectOffset padding = new RectOffset(2, 2, 5, 5);
                rect = padding.Remove(rect);
                int[] values = new int[boundComponent.MaxLods];
                for (int i = 0; i < boundComponent.MaxLods; i++)
                {
                    values[i] = boundComponent.GetTotalBitsOfLOD(i);
                }
                CoherenceArchetypeDrawer.DrawDataWeightMiniBar(rect, values, boundComponent.MaxLods, true);
            }
        }

        internal override void DrawRowBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, BindingsWindowSettings.ComponentRowColor);
            base.DrawRowBackground(rect);
        }

        protected override void DrawLeftBar(Rect rect)
        {
            //Rect nameRect = new Rect(rect.x + 10, rect.y, rect.width - 32, rect.height);
            Rect nameRect = new Rect(rect.x + 10, rect.y, rect.width-10, rect.height);

            GUIContent header = new GUIContent(displayName, UIHelpers.GetIcon(boundComponent.ComponentFullName));
            if (MissingScript)
            {
                EditorGUI.HelpBox(nameRect, "The associated script cannot be loaded.", MessageType.Warning);
            }
            else
            {
                GUI.Label(nameRect, header, EditorStyles.boldLabel);
            }

            DrawSelectionButtons(rect);
            SetLocalBindingEditing(tree.BindingsWindow.StateController.AllowUserToBind);

        }
        internal void DrawSelectionButtons(Rect rect)
        {
            RectOffset padding = new RectOffset(30, 1, 1, 2);
            Rect paddedRect = padding.Remove(rect);

            GUIStyle minitext = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            minitext.alignment = TextAnchor.MiddleLeft;


            int buttonSize = 16;

            Rect selectAllRect = new Rect(paddedRect.xMax - buttonSize * 2, paddedRect.y, buttonSize, buttonSize);
            Rect deselectAllRect = new Rect(selectAllRect.xMax, paddedRect.y, buttonSize, buttonSize);

            if (tree.BindingsWindow.DrawSelectAllButton(selectAllRect))
            {
                Undo.RecordObject(sync, "Selecting All");
                GetActiveGroup().SelectAllUnfilteredMembers(true, tree);
            }
            if (tree.BindingsWindow.DrawSelectNoneButton(deselectAllRect))
            {
                Undo.RecordObject(sync, "Deselecting All");
                GetActiveGroup().SelectAllUnfilteredMembers(false, tree);
            }
        }


        private void SetLocalBindingEditing(bool editingLocalBindings)
        {
            if (editingLocalBindings != EditingLocalBindings)
            {
                EditingLocalBindings = editingLocalBindings;
                if (editingLocalBindings && children != null)
                {
                    StoreExpandedState();
                    tree.SetExpandedRecursive(id, true);
                }

                if (!editingLocalBindings && expandedState != null)
                {
                    foreach (var kvp in expandedState)
                    {
                        _ = tree.SetExpanded(kvp.Key, kvp.Value);
                    }
                }

                tree.UpdateOnly();
            }
        }

        private Dictionary<int, bool> expandedState;

        private void StoreExpandedState()
        {
            var childrenIds = GetAllChildrenIDs();
            expandedState = new Dictionary<int, bool>();
            foreach (int childId in childrenIds)
            {
                expandedState[childId] = tree.IsExpanded(childId);
            }
        }

        protected override void DrawValueRangeBar(Rect rect)
        {
            base.DrawValueRangeBar(rect);
            if (!BindingsWindowSettings.CompactView)
            {
                BindingsTreeViewComponentGroup activeGroup = GetActiveGroup();
                activeGroup.DrawValueRangeBar(rect);
            }
        }

        protected override void DrawSampleRateBar(Rect rect)
        {
            base.DrawValueRangeBar(rect);
            if (!BindingsWindowSettings.CompactView)
            {
                BindingsTreeViewComponentGroup activeGroup = GetActiveGroup();
                activeGroup.DrawSampleRateBar(rect);
            }
        }

        protected override void DrawBindingConfigBar(Rect rect)
        {
            base.DrawBindingConfigBar(rect);
            BindingsTreeViewComponentGroup activeGroup = GetActiveGroup();
            activeGroup.DrawBindingConfigBar(rect);
        }

        protected override void DrawLOD(Rect rect, int step)
        {
            if (!boundComponent.HasSyncedBindings())
            {
                return;
            }

            if (!BindingsWindowSettings.CompactView)
            {
                var activeGroup = GetActiveGroup();
                activeGroup.DrawLOD(rect, step);
            }

            var isActive = boundComponent.LodStepsActive > step;
            var padding = new RectOffset(2, 2, 2, 2);
            var innerRect = padding.Remove(rect);
            var overrideToggleRect = new Rect(innerRect.x, innerRect.y, 20, innerRect.height);

            // The active toggle is not shown on Transform, because we shouldn't allow users to not sync position
            var drawActiveToggle = step > 0 && boundComponent.Component.GetType() != typeof(Transform);

            if (drawActiveToggle)
            {
                EditorGUI.BeginChangeCheck();
                var style = new GUIStyle(GUIStyle.none) { alignment = TextAnchor.UpperLeft };
                var overrideContent = isActive ? Icons.GetContent("Coherence.Sync", "Syncing on this LOD") : Icons.GetContent("Coherence.SyncInactive", "Not Syncing on this LOD");
                EditorGUIUtility.SetIconSize(new(12, 12));
                isActive = GUI.Toggle(overrideToggleRect, isActive, overrideContent, style);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sync, "Toggle LOD Active");
                    SetIsActive(isActive, step);
                    tree.BindingsWindow.OnPropertyValueChanged();
                    tree.BindingsWindow.UpdateSerialization();
                }
            }

            if (isActive)
            {
                var bitsDisplayRect = new Rect(innerRect.xMax - 100, innerRect.y, 100, innerRect.height);
                var bits = boundComponent.GetTotalBitsOfLOD(step);

                if (BindingsWindowSettings.ShowBitPercentages)
                {
                    // Rect changes
                    var width = BindingsWindowSettings.LODBitPercentageWidth;
                    bitsDisplayRect.x -= width;
                    var textRect = new Rect(rect.xMax - width, rect.y, width, rect.height);
                    var totalBitsOfStep = sync.Archetype.GetTotalActiveBitsOfLOD(step);
                    DrawBitPercentage(textRect, bits, totalBitsOfStep, "", " of the bits at this LOD");
                }

                var bitsText = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight, richText = true };
                GUI.Label(bitsDisplayRect, $"<size=12><b>{bits}</b></size> {(bits == 1 ? "Bit" : "Bits")}", bitsText);
            }
        }

        private void SetIsActive(bool isActive, int lodStep) => boundComponent.SetLodActive(isActive, lodStep);

        internal override bool CanChangeExpandedState() => EditorCache.GetComponentDescriptors(boundComponent.Component).Count > 0;

        internal override void UpdateExpandedState(bool expanded)
        {
            boundComponent.ExpandedInEditor = expanded;
            base.UpdateExpandedState(expanded);
        }

        private BindingsTreeViewComponentGroup GetActiveGroup() => tree.BindingsWindow.StateController.Methods ? Methods : Fields;
    }
}
