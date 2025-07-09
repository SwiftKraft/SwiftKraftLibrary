// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Coherence.Toolkit;
    using System;
    using Coherence.Editor.Toolkit;
    using Coherence.Toolkit.Bindings;

    internal class BindingsTreeViewOrphanedItem : BindingsTreeViewItem
    {
        // This row will remain even after deleted to minimize rebuilds
        public bool Deleted { private set; get; }

        private Binding binding;
        private BindingsTreeViewOrphanedGroup group;

        private CoherenceSync sync;

        internal bool IsMethod { private set; get; }
        internal SchemaType SchemaType { private set; get; }

        internal BindingsTreeViewOrphanedItem(int id, BindingsTreeViewOrphanedGroup group, Binding binding, CoherenceSync sync)
        {
            SharedConstructor(id, group, sync);

            this.binding = binding;

            displayName = binding.Name;
            IsMethod = false;
            TryGetSchemaType(binding.MonoAssemblyRuntimeType);
        }

        private void TryGetSchemaType(Type type)
        {
            SchemaType = type == null ? SchemaType.Unknown : TypeUtils.GetSchemaType(type);
        }

        private void SharedConstructor(int id, BindingsTreeViewOrphanedGroup group, CoherenceSync sync)
        {
            // TreeviewSpecific
            this.id = id;
            this.group = group;
            group.AddChild(this);
            group.AddMember(this);
            depth = 1;

            this.sync = sync;
            Setup(rowHeight: 20, lodSteps: sync.Archetype.LODLevels.Count);
        }

        protected override void DrawLeftBar(Rect rect)
        {
            // Get rects
            Rect contentRect = new Rect(rect.x + 15, rect.y, rect.width - 40, rect.height);
            Rect buttonrect = new Rect(rect.xMax - 16, rect.y + 2, 16, 16);

            // Draw label/ bindings
            GUIContent content  = ContentUtils.GetInvalidContent(binding.Descriptor, "Component is missing.");

            GUI.Label(contentRect, content, ContentUtils.GUIStyles.richLabel);

            if (GUI.Button(buttonrect, EditorGUIUtility.IconContent("TreeEditor.Trash"), GUIStyle.none))
            {
                Undo.RecordObject(sync, "Delete Orphaned Binding");
                Remove();
                group.RemoveMember(this);
                EditorUtility.SetDirty(sync);
                Undo.FlushUndoRecordObjects();
            }
        }

        internal void Remove()
        {
            _ = CoherenceSyncBindingHelper.RemoveBinding(sync, binding.UnityComponent, binding.Descriptor);
            Deleted = true;
        }

        internal override bool CheckIfFilteredOut(BindingsWindowTreeFilters filters, bool bindingCanBeEdited)
        {
            if (Deleted)
            {
                return true;
            }

            return filters.FilterOutBinding(SchemaType, displayName);
        }
    }
}
