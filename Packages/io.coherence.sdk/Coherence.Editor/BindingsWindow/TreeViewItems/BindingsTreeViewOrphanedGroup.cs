// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using UnityEditor.IMGUI.Controls;
    using System;

    internal class BindingsTreeViewOrphanedGroup : BindingsTreeViewItem
    {
        public BindingsTreeViewHiddenRowsItem HiddenField { get; set; }
        private BindingsWindowTree tree;
        private CoherenceSync sync;
        private List<BindingsTreeViewOrphanedItem> members = new List<BindingsTreeViewOrphanedItem>();

        internal BindingsTreeViewOrphanedGroup(int id, TreeViewItem assingTo, CoherenceSync sync, BindingsWindowTree tree)
        {
            // TreeviewSpecific
            this.id = id;
            assingTo.AddChild(this);
            displayName = "Orphaned bindings";
            depth = 1;

            this.sync = sync;
            this.tree = tree;

            Setup(rowHeight: 20, lodSteps: sync.Archetype.LODLevels.Count);
        }

        private void DrawBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, BindingsWindowSettings.ComponentRowColor);
        }

        internal void AddMember(BindingsTreeViewOrphanedItem member)
        {
            members.Add(member);
        }

        internal void RemoveMember(BindingsTreeViewOrphanedItem member)
        {
            _ = members.Remove(member);
            UpdateTree();
        }

        internal override void DrawRowBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, BindingsWindowSettings.ComponentRowColor);
            base.DrawRowBackground(rect);
        }

        protected override void DrawLeftBar(Rect rect)
        {
            Rect nameRect = new Rect(rect.x + 10, rect.y, rect.width, rect.height);
            Rect editRect = new Rect(rect.xMax - 16, rect.y + 2, 16, 16);

            GUI.Label(nameRect, EditorGUIUtility.TrTextContentWithIcon("Orphaned Bindings", "Warning"), EditorStyles.boldLabel);

            // Local field editing
            if (GUI.Button(editRect, EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Remove All Orphaned Bindings"), GUIStyle.none))
            {
                RemoveAll();
            }
        }

        private Tuple<int, bool> previousExpandedView;
        private Dictionary<int, bool> expandedState;

        private void RemoveAll()
        {
            Undo.RecordObject(sync, "Deleted All Orphaned Bindings");

            foreach (var member in members)
            {
                member.Remove();
            }
            members.Clear();
            UpdateTree();
            EditorUtility.SetDirty(sync);
            Undo.FlushUndoRecordObjects();

        }

        private void UpdateTree()
        {
            if (members.Count == 0)
            {
                tree.Reload();
            }
            else
            {
                tree.UpdateOnly();
            }
        }

        internal override bool CanChangeExpandedState()
        {
            return members.Count > 0;
        }
    }
}
