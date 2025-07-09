// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using UnityEditor.IMGUI.Controls;
    using System.Linq;
    using Toolkit;
    using Coherence.Toolkit.Bindings;

    internal class BindingsWindowTree : TreeView
    {
        internal bool ExpandedChanged { set; get; }

        internal CoherenceSync Sync { private set; get; }
        internal BindingsWindow BindingsWindow { private set; get; }

        private List<BindingsTreeViewItem> AllItems;
        private TreeViewItem root;
        private TreeViewItem updateRow;

        protected override bool CanMultiSelect(TreeViewItem item) => true;

        protected override bool CanChangeExpandedState(TreeViewItem item)
        {
            BindingsTreeViewItem bindingsTreeViewItem = (BindingsTreeViewItem)item;

            return bindingsTreeViewItem != null ? bindingsTreeViewItem.CanChangeExpandedState() : item.hasChildren;
        }

        protected override void ExpandedStateChanged()
        {
            foreach (BindingsTreeViewItem item in AllItems)
            {
                item.UpdateExpandedState(IsExpanded(item.id));
            }

            ExpandedChanged = true;

            if (BindingsWindowSettings.AutoSave)
            {
                BindingsWindow.UpdateSerialization();
            }
        }

        internal BindingsWindowTree(TreeViewState state, BindingsWindow window, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            BindingsWindow = window;

            showAlternatingRowBackgrounds = false;
            showBorder = false;
            extraSpaceBeforeIconAndLabel = 0;
            cellMargin = 0;
            rowHeight = 22;
            Reload();
            BindingsTreeViewBindingItem.OnInput += HandleInput;
        }

        internal void Dispose() => BindingsTreeViewBindingItem.OnInput -= HandleInput;

        internal void UpdateOnly()
        {
            bool expanded = IsExpanded(updateRow.id);
            _ = SetExpanded(updateRow.id, !expanded);
        }

        protected override TreeViewItem BuildRoot()
        {
            root = new TreeViewItem(-1, -1);
            Sync = GetSelectedSync();
            int id = 0;

            AllItems = new List<BindingsTreeViewItem>();

            if (Sync)
            {
                Sync.ValidateArchetype();
                // Get all the known bindings
                for (int i = 0; i < Sync.Archetype.BoundComponents.Count; i++)
                {
                    // Add component
                    var boundComponent = Sync.Archetype.BoundComponents[i];
                    if (boundComponent.GetTotalActiveBindings() - boundComponent.GetTotalActiveMethodBindings() == 0)
                    {
                        continue;
                    }

                    var newComponentRow = new BindingsTreeViewComponentItem(id, root, Sync, boundComponent, this);
                    newComponentRow.UpdateExpandedState(boundComponent.ExpandedInEditor);
                    _ = SetExpanded(id++, boundComponent.ExpandedInEditor);
                    AllItems.Add(newComponentRow);

                    // Add rows for fields

                    Component component = boundComponent.Component;
                    if (!EditorCache.GetBindingProviderForComponent(component, out DescriptorProvider provider))
                    {
                        Debug.LogError($"Missing Binding Provider for: {component}, expected to fallback to DefaultBindingProvider. If needed you can implement your own {nameof(DescriptorProvider)}");
                    }

                    var bindings = GetAllValidBindings(component);
                    var obsoleteBindings = GetAllObsoleteBindings(component);

                    bool drawBindings = bindings.Count > 0 || obsoleteBindings.Count > 0;
                    var drawActiveOnly = !BindingsWindow.EditingAllFields && !newComponentRow.EditingLocalBindings;

                    bool bindingsDrawn = false;

                    int methodCount = bindings.Count(x => x.IsMethod);
                    bool drawFieldBindings = bindings.Count > methodCount || obsoleteBindings.Count > 0;

                    if (drawFieldBindings && !BindingsWindow.StateController.Methods)
                    {
                        bindingsDrawn = true;

                        // fields
                        foreach (var binding in bindings)
                        {
                            if (binding.IsMethod)
                            {
                                continue;
                            }

                            SchemaType schemaType = TypeUtils.GetSchemaType(binding.MonoAssemblyRuntimeType);
                            binding.CreateArchetypeData(schemaType, boundComponent.MaxLods);
                            var newBindingRow = new BindingsTreeViewBindingItem(Sync, boundComponent, binding);
                            newBindingRow.SetTreeViewData(id++, newComponentRow, BindingsWindow);
                            AllItems.Add(newBindingRow);
                        }

                        // obsolete fields

                        if (obsoleteBindings.Count > 0)
                        {
                            var obsoleteGroupRow = new BindingsTreeViewObsoleteGroupItem(id, newComponentRow, Sync, boundComponent, BindingsWindow, false);
                            AllItems.Add(obsoleteGroupRow);
                            id++;

                            foreach (var binding in obsoleteBindings)
                            {
                                var newBindingRow = new BindingsTreeViewObsoleteBinding(Sync, binding, provider);
                                newBindingRow.SetTreeViewData(id++, newComponentRow, obsoleteGroupRow);
                                AllItems.Add(newBindingRow);
                                AllItems.Add(newBindingRow);
                            }
                        }
                    }

                    // methods
                    if (methodCount > 0 && BindingsWindow.StateController.Methods)
                    {
                        bindingsDrawn = true;

                        foreach (var binding in bindings)
                        {
                            if (binding.IsMethod)
                            {
                                var newBindingRow = new BindingsTreeViewBindingItem(Sync, boundComponent, binding);
                                newBindingRow.SetTreeViewData(id++, newComponentRow, BindingsWindow);
                            }
                        }
                    }

                    if (bindingsDrawn)
                    {
                        // Add row for hidden items
                        var hiddenRow = new BindingsTreeViewHiddenRowsItem(id++, newComponentRow, boundComponent, newComponentRow);
                        AllItems.Add(hiddenRow);
                    }
                }

                // Get any orphaned bindings
                List<Binding> orphanedBindings = GetOrphanedBindings();

                // Draw orphaned bindings
                if (orphanedBindings.Count > 0)
                {
                    var orphanedGroup = new BindingsTreeViewOrphanedGroup(id, root, Sync, this);
                    _ = SetExpanded(id++, false);
                    AllItems.Add(orphanedGroup);

                    foreach (var binding in orphanedBindings)
                    {
                        var newOrphanedBindingRow = new BindingsTreeViewOrphanedItem(id++, orphanedGroup, binding, Sync);
                        AllItems.Add(newOrphanedBindingRow);
                    }
                }
            }

            // this item is used since its the easiest way to trigger an update the tree without rebuilding the root
            updateRow = new TreeViewItem(id++, 1, "");
            updateRow.parent = root;

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        internal void RebuildRows() => SetSelection(new List<int> { 1 });

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            IList<TreeViewItem> visible = new List<TreeViewItem>();
            if (root.hasChildren)
            {
                foreach (var child in root.children)
                {
                    GetVisibleChildrenByExpansion(visible, GetExpanded(), child);
                }
            }

            IList<TreeViewItem> matchedItems = new List<TreeViewItem>();
            IList<BindingsTreeViewBindingItem> hiddenItems = new List<BindingsTreeViewBindingItem>();
            IList<BindingsTreeViewHiddenRowsItem> hiddenItemRows = new List<BindingsTreeViewHiddenRowsItem>();

            // handle expanded items

            foreach (var item in AllItems)
            {
                if (visible.Contains(item))
                {
                    BindingsTreeViewBindingItem bindingItem = item as BindingsTreeViewBindingItem;
                    BindingsTreeViewHiddenRowsItem hiddenItem = item as BindingsTreeViewHiddenRowsItem;

                    // Dont show unsynced fields unless in we are in edit bindings mode
                    bool bindingCanBeEdited = false;
                    if (bindingItem != null)
                    {
                        bindingCanBeEdited = BindingsWindow.EditingAllFields || bindingItem.ComponentItem.EditingLocalBindings;
                        if (!bindingItem.SelectedForSync && !bindingCanBeEdited)
                        {
                            continue;
                        }
                    }

                    bool filteredOut = item.CheckIfFilteredOut(BindingsWindow.Toolbar.Filters, bindingCanBeEdited);

                    if (hiddenItem != null)
                    {
                        hiddenItem.Reset();
                        hiddenItemRows.Add(hiddenItem);
                    }

                    if (!filteredOut)
                    {
                        matchedItems.Add(item);
                    }
                    else if (bindingItem != null)
                    {
                        hiddenItems.Add(bindingItem);
                    }
                }
            }

            foreach (var filteredFieldItem in hiddenItems)
            {
                filteredFieldItem.ComponentItem.HiddenField.Add(filteredFieldItem);
            }

            foreach (var hiddenRow in hiddenItemRows)
            {
                if (hiddenRow.HiddenItems == 0 && matchedItems.Contains(hiddenRow))
                {
                    _ = matchedItems.Remove(hiddenRow);
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return matchedItems;
        }

        internal void SetActiveStateOfAllUnfilteredBindings(bool shouldBeEnabled)
        {
            foreach (var item in AllItems)
            {
                BindingsTreeViewBindingItem bindingItem = item as BindingsTreeViewBindingItem;
                if (bindingItem != null)
                {
                    bool filteredOut = item.CheckIfFilteredOut(BindingsWindow.Toolbar.Filters, true);
                    if (!filteredOut)
                    {
                        bindingItem.SetBindingActive(shouldBeEnabled);
                    }
                }
            }
        }

        internal void GetVisibleChildrenByExpansion(IList<TreeViewItem> visible, IList<int> expanded, TreeViewItem item)
        {
            visible.Add(item);
            bool isExpanded = expanded.Contains(item.id);
            BindingsTreeViewItem syncTreeViewItem = (BindingsTreeViewItem)item;
            //syncTreeViewItem?.SetExpanded(isExpanded);

            if (isExpanded && item.children != null)
            {
                foreach (var child in item.children)
                {
                    if (child != null)
                    {
                        GetVisibleChildrenByExpansion(visible, expanded, child);
                    }
                }
            }
        }

        internal void FocusOnLOD(int lodStep)
        {
            int column = BindingsWindow.StateController.GetColumnByLOD(lodStep);
            Rect columnRect = multiColumnHeader.GetColumnRect(column);
            float xPos = columnRect.center.x - (treeViewRect.xMax * .5f);
            xPos = Mathf.Clamp(xPos, 0, multiColumnHeader.state.widthOfAllVisibleColumns - treeViewRect.xMax);
            state.scrollPos = new Vector2(xPos, state.scrollPos.y);
        }

        private CoherenceSync GetSelectedSync() => BindingsWindow.Component;

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            BindingsTreeViewItem bindingsItem = (BindingsTreeViewItem)item;
            return bindingsItem.RowHeight;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (BindingsTreeViewItem)args.item;

            float rowTotalWidth = treeViewRect.width + state.scrollPos.x;
            Rect rowRect = new Rect(args.rowRect.x, args.rowRect.y, rowTotalWidth, args.rowRect.height);

            item.DrawRowBackground(rowRect);

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                Rect rect = args.GetCellRect(i);
                int columnIndex = args.GetColumn(i);

                BindingsWindowState.ColumnContent columnContent = BindingsWindow.StateController.GetColumnContent(columnIndex);
                int lodstep = BindingsWindow.StateController.GetLODByColumnIndex(columnIndex);
                item.DrawCell(rect, columnContent, lodstep);
            }
        }

        protected override void AfterRowsGUI()
        {
            base.AfterRowsGUI();
            BindingsTreeViewInput.CheckFocus();
        }

        private List<BindingsTreeViewBindingItem> selectedBindings = new List<BindingsTreeViewBindingItem>();

        // Making rows non selectable
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            foreach (BindingsTreeViewBindingItem selected in selectedBindings)
            {
                selected.SelectedInTreeView = false;
            }

            selectedBindings.Clear();

            if (selectedIds.Count < 1)
            {
                return;
            }

            List<int> validBindings = new List<int>();
            foreach (int id in selectedIds)
            {
                var item = FindItem(id, root);
                BindingsTreeViewBindingItem bindingItem = item as BindingsTreeViewBindingItem;
                if (bindingItem != null)
                {
                    validBindings.Add(id);
                    selectedBindings.Add(bindingItem);
                    bindingItem.SelectedInTreeView = true;
                }
            }

            SetSelection(validBindings);
            RefreshCustomRowHeights();
        }

        private void HandleInput(BindingsTreeViewInput input, BindingsTreeViewBindingItem fromBinding)
        {
            Undo.RecordObject(Sync, input.UndoMessage);

            var autoSave = BindingsWindowSettings.AutoSave;
            if (selectedBindings.Count > 0 && selectedBindings.Contains(fromBinding))
            {
                foreach (var selectedBinding in selectedBindings)
                {
                    selectedBinding.ApplyInputToBinding(input, autoSave);
                }
            }
            else
            {
                fromBinding.ApplyInputToBinding(input, autoSave);
            }

            BindingsWindow.OnPropertyValueChanged();

            Undo.FlushUndoRecordObjects();
        }

        // Find Orphans

        private List<Binding> GetOrphanedBindings()
        {
            List<Binding> orphanedBindings = new List<Binding>();
            foreach (var binding in Sync.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (!binding.unityComponent)
                {
                    orphanedBindings.Add(binding);
                }
            }

            return orphanedBindings;
        }


        private List<Binding> GetAllValidBindings(Component component)
        {
            List<Binding> validbindings = new List<Binding>();

            var descriptors = EditorCache.GetComponentDescriptors(component);

            foreach (var binding in Sync.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (binding.unityComponent == component && EditorCache.DescriptorExistsForBinding(descriptors, binding))
                {
                    validbindings.Add(binding);
                }
            }

            return validbindings;
        }

        private List<Binding> GetAllObsoleteBindings(Component component)
        {
            List<Binding> obsoleteBindings = new List<Binding>();
            var descriptors = EditorCache.GetComponentDescriptors(component);

            // iterate through custom bindings currently serialized,
            // that are not registered anymore for this component
            foreach (var binding in Sync.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (binding.unityComponent != component)
                {
                    continue;
                }

                if (!EditorCache.DescriptorExistsForBinding(descriptors, binding))
                {
                    obsoleteBindings.Add(binding);
                }
            }

            return obsoleteBindings;
        }
    }
}
