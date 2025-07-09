// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using Coherence.Editor.Toolkit;
    using Coherence.Toolkit.Archetypes;

    internal class BindingsTreeViewObsoleteGroupItem : BindingsTreeViewItem
    {
        private BindingsTreeViewComponentItem belongsTo;
        private CoherenceSync sync;
        private BindingsWindow window;

        private List<BindingsTreeViewObsoleteBinding> members = new List<BindingsTreeViewObsoleteBinding>();

        internal BindingsTreeViewObsoleteGroupItem(int id, BindingsTreeViewComponentItem belongsTo, CoherenceSync sync, ArchetypeComponent boundComponent, BindingsWindow window, bool isMethodGroup)
        {
            // TreeviewSpecific
            this.id = id;
            this.belongsTo = belongsTo;
            belongsTo.AddChild(this);

            displayName = boundComponent.DisplayName;
            depth = 1;

            this.sync = sync;
            this.window = window;

            Setup(rowHeight: 20, lodSteps: boundComponent.MaxLods);
        }

        internal void AddMember(BindingsTreeViewObsoleteBinding member)
        {
            members.Add(member);
        }

        protected override void DrawLeftBar(Rect rect)
        {
            RectOffset padding = new RectOffset(30, 5, 5, 5);
            Rect labelrect = padding.Remove(rect);

            GUIStyle minitext = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            minitext.alignment = TextAnchor.MiddleLeft;

            GUI.Label(labelrect, "Obsolete Fields", minitext);

            bool bindingsCanBeEdited = BindingsWindow.EditingAllFields || belongsTo.EditingLocalBindings;
            if (bindingsCanBeEdited)
            {
                int buttonSize = 80;

                Rect removeAllRect = new Rect(rect.xMax - buttonSize * 2, rect.y + 1, buttonSize, rect.height - 2);
                bool anyFilterActive = window.Toolbar.Filters.AnyFilterActive();

                string selectAllText = anyFilterActive ? "Select Visible" : "Select All";

                if (GUI.Button(removeAllRect, new GUIContent(selectAllText)))
                {
                    Undo.RecordObject(sync, "Selecting All");
                    RemoveAllAllUnfilteredMembers();
                }
            }
        }

        private void RemoveAllAllUnfilteredMembers()
        {
            foreach (var member in members)
            {
                if (!member.CheckIfFilteredOut(window.Toolbar.Filters, true))
                {
                    member.Remove();
                }
            }
        }

        internal override bool CanChangeExpandedState()
        {
            return members.Count > 0;
        }
    }
}
