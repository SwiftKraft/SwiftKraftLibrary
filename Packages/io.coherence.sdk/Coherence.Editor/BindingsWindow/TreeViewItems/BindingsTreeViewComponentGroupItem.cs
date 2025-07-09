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

    internal class BindingsTreeViewComponentGroup
    {
        private BindingsTreeViewComponentItem belongsTo;
        private ArchetypeComponent boundComponent;
        private CoherenceSync sync;
        private bool isMethodGroup;
        private BindingsWindow window;

        private List<BindingsTreeViewBindingItem> members = new List<BindingsTreeViewBindingItem>();

        internal BindingsTreeViewComponentGroup(BindingsTreeViewComponentItem belongsTo, CoherenceSync sync, ArchetypeComponent boundComponent, BindingsWindow window, bool isMethodGroup)
        {
            this.belongsTo = belongsTo;

            this.boundComponent = boundComponent;
            this.sync = sync;
            this.isMethodGroup = isMethodGroup;
            this.window = window;
        }

        internal void AddMember(BindingsTreeViewBindingItem member)
        {
            members.Add(member);
        }
        internal void SelectAllUnfilteredMembers(bool active, BindingsWindowTree tree)
        {
            List<int> selection = new List<int>( tree.GetSelection());
            foreach (var member in members)
            {
                if (!member.CheckIfFilteredOut(window.Toolbar.Filters, true))
                {
                    if (active && !selection.Contains(member.id) && member.SelectedForSync)
                    {
                        selection.Add(member.id);
                    }
                    if (!active && selection.Contains(member.id) && member.SelectedForSync)
                    {
                        selection.Remove(member.id);
                    }
                }
            }
            tree.SetSelection(selection, UnityEditor.IMGUI.Controls.TreeViewSelectionOptions.FireSelectionChanged);
        }

        internal void DrawValueRangeBar(Rect rect)
        {
            if (ShouldDrawHeaders() && !isMethodGroup)
            {
                RectOffset padding = new RectOffset(2, 2, 2, 2);
                BindingsTreeViewItem.SplitInTwoLayoutRects layout = new BindingsTreeViewItem.SplitInTwoLayoutRects(rect, padding);

                GUIStyle minitext = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                minitext.alignment = TextAnchor.LowerLeft;

                GUI.Label(layout.FirstRect, "Min", minitext);
                GUI.Label(layout.SecondRect, "Max", minitext);
            }
        }
        
        internal void DrawSampleRateBar(Rect rect)
        {
            if (ShouldDrawHeaders() && !isMethodGroup)
            {
                GUIStyle minitext = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                minitext.alignment = TextAnchor.LowerLeft;

                GUI.Label(rect, "Frequency", minitext);
            }
        }

        internal void DrawBindingConfigBar(Rect rect)
        {
            if (ShouldDrawHeaders())
            {
                GUIStyle minitext = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                minitext.alignment = TextAnchor.MiddleLeft;
                RectOffset padding = new RectOffset(2, 2, 2, 2);

                if (isMethodGroup)
                {
                    GUI.Label(padding.Remove(rect), "Routing", minitext);
                }
                else
                {
                    GUI.Label(padding.Remove(rect), "Interpolation", minitext);
                }
            }
        }

        internal void DrawLOD(Rect rect, int step)
        {
            bool enabledOnComponent = boundComponent.LodStepsActive > step;

            if (enabledOnComponent && !isMethodGroup && ShouldDrawHeaders())
            {
                GUIStyle minitext = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                minitext.alignment = TextAnchor.LowerLeft;

                BindingsTreeViewItem.LODLayoutRects layout = new BindingsTreeViewItem.LODLayoutRects(rect, BindingsWindowSettings.CanEditLODRanges);

                GUI.Label(layout.MinRange, "MinValue", minitext);
                GUI.Label(layout.MaxRange, "MaxValue", minitext);
                GUI.Label(layout.Bits, "Bits", minitext);
                GUI.Label(layout.Precision, "Precision", minitext);
            }
        }

        private bool ShouldDrawHeaders()
        {
            if (!belongsTo.Expanded)
            {
                return false;
            }

            if (BindingsWindow.EditingAllFields || belongsTo.EditingLocalBindings)
            {
                return true;
            }

            foreach (var member in members)
            {
                if (member.SelectedForSync)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
