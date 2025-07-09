// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Coherence.Toolkit;
    using Coherence.Editor.Toolkit;
    using Coherence.Toolkit.Bindings;

    internal class BindingsTreeViewObsoleteBinding : BindingsTreeViewItem
    {
        public BindingsTreeViewComponentItem ComponentItem { private set; get; }
        public bool Selected { private set; get; }

        private Binding binding;
        private CoherenceSync sync;
        private DescriptorProvider provider;

        internal SchemaType SchemaType { private set; get; }

        internal BindingsTreeViewObsoleteBinding(CoherenceSync sync, Binding binding, DescriptorProvider provider)
        {
            this.sync = sync;
            this.provider = provider;
            Setup(rowHeight: 24, lodSteps: sync.Archetype.LODLevels.Count);

            this.binding = binding;
            displayName = binding.Name;
            SchemaType = TypeUtils.GetSchemaType(this.binding.MonoAssemblyRuntimeType);
        }

        internal void SetTreeViewData(int id, BindingsTreeViewComponentItem componentItem, BindingsTreeViewObsoleteGroupItem group)
        {
            this.id = id;
            group.AddChild(this);
            group.AddMember(this);
            depth = 2;
            ComponentItem = componentItem;
        }

        protected override void DrawLeftBar(Rect rect)
        {
            // Get rects
            Rect contentRect = new Rect(rect.x + 23, rect.y, rect.width - 23, rect.height);
            Rect buttonrect = new Rect(contentRect.xMax - 24, rect.y + ((rect.height - 20) * .5f), 24, 20);

            // Draw label/ bindings
            GUIContent content = new GUIContent();
            var reason = $"BindingProvider `{provider.GetType().Name}` doesn't handle this binding.";
            content = ContentUtils.GetInvalidContent(binding.Descriptor, reason);

            if (GUI.Button(buttonrect, EditorGUIUtility.IconContent("TreeEditor.Trash")))
            {
                Undo.RecordObject(sync, "Delete Orphaned Binding");
                Remove();
                EditorUtility.SetDirty(sync);
                Undo.FlushUndoRecordObjects();
            }
            else
            {
                GUI.Label(contentRect, content, ContentUtils.GUIStyles.richLabel);
            }
        }

        internal void Remove()
        {
            Undo.RecordObject(sync, "Delete Custom Binding");
            _ = CoherenceSyncBindingHelper.RemoveBinding(sync, binding.unityComponent, binding.Descriptor);
            EditorUtility.SetDirty(sync);
            Undo.FlushUndoRecordObjects();
        }

        internal override bool CheckIfFilteredOut(BindingsWindowTreeFilters filters, bool bindingCanBeEdited)
        {
            return filters.FilterOutBinding(SchemaType, displayName);
        }
    }
}
