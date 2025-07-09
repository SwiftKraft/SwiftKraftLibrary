// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class CoherenceSyncObjectsTreeView : TreeView
    {
        private List<Type> objectProviderTypes;
        private List<Type> objectInstantiatorTypes;

        private Dictionary<NetworkObjectEntryTreeViewItem, SyncSerializedData> serializedData = new();

        private ConfigsAnalyzerHandler analyzer;

        private bool selectionOriginatedFromTreeView;

        private const string ColumnVisibilityPrefix = "Coherence.VisibleColumn.";

        public bool EditMode { get; internal set; }

        private struct SyncSerializedData
        {
            public CoherenceSync Sync;
            public SerializedObject SerializedObject;
            public SerializedProperty SimulationProperty;
            public SerializedProperty AuthorityTransferProperty;
            public SerializedProperty LifetimeProperty;

            public SyncSerializedData(NetworkObjectEntryTreeViewItem item)
            {
                Sync = item.entry is { } config ? config.Sync : null;

                if (Sync)
                {
                    SerializedObject = new SerializedObject(Sync);
                    SimulationProperty = SerializedObject.FindProperty("simulationType");
                    AuthorityTransferProperty = SerializedObject.FindProperty(nameof(CoherenceSync.authorityTransferConfig));
                    LifetimeProperty = SerializedObject.FindProperty("lifetimeType");
                }
                else
                {
                    SerializedObject = null;
                    SimulationProperty = null;
                    AuthorityTransferProperty = null;
                    LifetimeProperty = null;
                }
            }
        }

        public CoherenceSyncObjectsTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader,
            ConfigsAnalyzerHandler analyzer) : base(state, multiColumnHeader)
        {
            this.analyzer = analyzer;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            EditorApplication.projectChanged += Reload;
            Undo.undoRedoPerformed += Reload;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.visibleColumnsChanged += MultiColumnHeaderOnVisibleColumnsChanged;
            Selection.selectionChanged += SelectionChanged;
            CoherenceSyncUtils.OnBeforeBindingAdded += OnBindingChanged;
            CoherenceSyncUtils.OnBeforeBindingRemoved += OnBindingChanged;

            objectProviderTypes = INetworkObjectDrawer.GatherObjectProviders(typeof(INetworkObjectProvider));
            objectInstantiatorTypes = INetworkObjectDrawer.GatherObjectProviders(typeof(INetworkObjectInstantiator));

            SetInitialColumnVisibility(multiColumnHeader);
        }

        private void OnBindingChanged(CoherenceSync sync, Binding binding)
        {
            EditorApplication.delayCall -= Reload;
            EditorApplication.delayCall += Reload;
        }

        private static void SetInitialColumnVisibility(MultiColumnHeader multiColumnHeader)
        {
            var visibleColumns = new List<int>();
            foreach (int column in Enum.GetValues(typeof(ColumnId)))
            {
                var visible = EditorPrefs.GetBool($"{ColumnVisibilityPrefix}{column.ToString()}", true);

                if (!multiColumnHeader.GetColumn(column).allowToggleVisibility)
                {
                    visible = true;
                }

                if (visible)
                {
                    visibleColumns.Add(column);
                }
            }

            multiColumnHeader.state.visibleColumns = visibleColumns.ToArray();
        }

        private void MultiColumnHeaderOnVisibleColumnsChanged(MultiColumnHeader multiColumnHeader)
        {
            foreach (int column in Enum.GetValues(typeof(ColumnId)))
            {
                EditorPrefs.SetBool($"{ColumnVisibilityPrefix}{column.ToString()}",
                    multiColumnHeader.IsColumnVisible(column));
            }

            multiColumnHeader.ResizeToFit();
        }

        public void OnDisable()
        {
            EditorApplication.projectChanged -= Reload;
            Undo.undoRedoPerformed -= Reload;
            multiColumnHeader.sortingChanged -= OnSortingChanged;
            multiColumnHeader.visibleColumnsChanged -= MultiColumnHeaderOnVisibleColumnsChanged;
            Selection.selectionChanged -= SelectionChanged;
            CoherenceSyncUtils.OnBeforeBindingAdded -= OnBindingChanged;
            CoherenceSyncUtils.OnBeforeBindingRemoved -= OnBindingChanged;
        }

        private void SelectionChanged()
        {
            if (selectionOriginatedFromTreeView)
            {
                selectionOriginatedFromTreeView = false;
                return;
            }

            var objs = Selection.objects;

            var networkObjs = new List<Object>();
            var selectedIds = new List<int>();

            foreach (var obj in objs)
            {
                if (!CoherenceSyncConfigUtils.TryGetFromAsset(obj, out _))
                {
                    continue;
                }

                networkObjs.Add(obj);
            }

            foreach (var item in rootItem.children)
            {
                foreach (var networkObj in networkObjs)
                {
                    if (item is NetworkObjectEntryTreeViewItem treeItem && treeItem.entry.EditorTarget == networkObj)
                    {
                        selectedIds.Add(treeItem.id);
                    }
                }
            }

            if (selectedIds.Count > 0)
            {
                SetSelection(selectedIds);
                FrameItem(selectedIds[0]);
            }
        }

        private void AddRow(TreeViewItem root, List<TreeViewItem> items, CoherenceSyncConfig config)
        {
            var groupTreeViewItem = new NetworkObjectEntryTreeViewItem(config, 0);
            serializedData.Add(groupTreeViewItem, new SyncSerializedData(groupTreeViewItem));
            if (config == null || config.EditorTarget == null)
            {
                items.Insert(0, groupTreeViewItem);
            }
            else
            {
                if (analyzer.Validate(config))
                {
                    items.Add(groupTreeViewItem);
                }
                else
                {
                    items.Insert(0, groupTreeViewItem);
                }
            }

            root.AddChild(groupTreeViewItem);
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = new List<TreeViewItem>();
            var registry = CoherenceSyncConfigRegistry.Instance;
            serializedData.Clear();
            analyzer.RefreshConfigsInfo();

            for (var i = 0; i < registry.LeakedCount; i++)
            {
                var config = registry.GetLeakedAt(i);
                if (!IncludedInSearch(config))
                {
                    continue;
                }

                AddRow(root, rows, config);
            }

            foreach (var config in registry.Where(IncludedInSearch))
            {
                AddRow(root, rows, config);
            }

            return rows;
        }

        private bool IncludedInSearch(CoherenceSyncConfig config)
        {
            if (!config)
            {
                return false;
            }

            if (string.IsNullOrEmpty(searchString))
            {
                return true;
            }

            return config.name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   config.Sync.gameObject.name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override TreeViewItem BuildRoot() => new(-1, -1);

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                SetSelection(Array.Empty<int>(), TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }

        static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new[]
            {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };

            var counter = 0;
            retVal[counter].headerContent = EditorGUIUtility.TrTextContentWithIcon(string.Empty,
                "Prefab Status. Possible configuration problems will be shown in this column.",
                "UnityEditor.InspectorWindow");
            retVal[counter].minWidth = 30;
            retVal[counter].width = 30;
            retVal[counter].maxWidth = 30;
            retVal[counter].headerTextAlignment = TextAlignment.Center;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            retVal[counter].allowToggleVisibility = false;
            counter++;

            retVal[counter].headerContent = Icons.GetContent("Schema",
                "Include In Schema: If disabled, this Object will not be included in the Schema and it will not be synchronized with the network.");
            ;
            retVal[counter].minWidth = 30;
            retVal[counter].width = 30;
            retVal[counter].maxWidth = 30;
            retVal[counter].headerTextAlignment = TextAlignment.Center;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            retVal[counter].allowToggleVisibility = false;
            counter++;

            retVal[counter].headerContent = new GUIContent("Prefab", $"Prefab using {nameof(CoherenceSync)}");
            retVal[counter].minWidth = 47;
            retVal[counter].width = 150;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            retVal[counter].allowToggleVisibility = false;
            counter++;

            retVal[counter].headerContent = new GUIContent("Config", $"Linked {nameof(CoherenceSyncConfig)} asset");
            retVal[counter].minWidth = 47;
            retVal[counter].width = 100;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            retVal[counter].allowToggleVisibility = false;
            counter++;

            retVal[counter].headerContent = new GUIContent("Load via", "Method used to load the prefab in runtime.");
            retVal[counter].minWidth = 57;
            retVal[counter].width = 110;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Instantiate via",
                "Method used to instantiate the remote object in runtime.");
            retVal[counter].minWidth = 89;
            retVal[counter].width = 90;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Simulate", "Simulate where spawned (until transfer)");
            retVal[counter].minWidth = 58;
            retVal[counter].width = 100;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Lifetime", "Configure persistence.");
            retVal[counter].minWidth = 54;
            retVal[counter].width = 100;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Authority Transfer",
                "Configure how this entity will transfer authority.");
            retVal[counter].minWidth = 110;
            retVal[counter].width = 110;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;

            return retVal;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            var networkTreeViewItem = item as NetworkObjectEntryTreeViewItem;
            return base.DoesItemMatchSearch(item, search) || (networkTreeViewItem != null &&
                                                              networkTreeViewItem.entry.name.IndexOf(search,
                                                                  StringComparison.OrdinalIgnoreCase) >=
                                                              0);
        }


        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as NetworkObjectEntryTreeViewItem;

            if (item == null)
            {
                base.RowGUI(args);
            }

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i));
            }
        }

        private void CellGUI(Rect cellRect, NetworkObjectEntryTreeViewItem item, int column)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            var serializedDataForItem = this.serializedData[item];

            switch ((ColumnId)column)
            {
                case ColumnId.Status:
                    {
                        var content = IsInvalidEntry(item)
                            ? EditorGUIUtility.TrTextContentWithIcon(string.Empty, GetStatusTooltip(item), "Warning")
                            : EditorGUIUtility.TrTextContentWithIcon(string.Empty, string.Empty, "Installed");

                        var rect = cellRect;
                        rect.x = (rect.width * 0.5f) - (EditorStyles.label.CalcSize(content).x * 0.5f);

                        EditorGUI.LabelField(rect, content);
                    }
                    break;
                case ColumnId.IncludeInSchema:
                    {
                        if (IsMissingAsset(item))
                        {
                            break;
                        }

                        using var change = new EditorGUI.ChangeCheckScope();

                        var rect = cellRect;
                        rect.x += 5f;
                        item.entry.IncludeInSchema = EditorGUI.Toggle(rect, item.entry.IncludeInSchema);

                        if (change.changed)
                        {
                            EditorUtility.SetDirty(item.entry);
                            BakeUtil.CoherenceSyncSchemasDirty = true;
                        }
                    }
                    break;
                case ColumnId.Name:
                    {
                        var icon = !IsInvalidEntry(item)
                            ? AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(item.entry.EditorTarget))
                            : null;
                        EditorGUI.LabelField(cellRect, new GUIContent(item.displayName, icon));
                    }
                    break;
                case ColumnId.ConfigName:
                    {
                        var icon = !IsInvalidEntry(item)
                            ? AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(item.entry))
                            : null;
                        EditorGUI.LabelField(cellRect, new GUIContent(item.entry.name, icon));
                    }
                    break;
                case ColumnId.LoadVia:
                    {
                        if (IsMissingAsset(item))
                        {
                            break;
                        }

                        var typeName = item.entry.Provider != null
                            ? INetworkObjectDrawer.typeDisplayNames[item.entry.Provider.GetType().FullName ?? string.Empty]
                            : new GUIContent("Missing");
                        if (EditMode)
                        {
                            if (EditorGUI.DropdownButton(cellRect, typeName, FocusType.Passive,
                                    CoherenceHubLayout.Styles.PopupNonFixedHeight))
                            {
                                var so = new SerializedObject(item.entry);
                                var typesPopup = new INetworkObjectDrawer.TypesPopup(objectProviderTypes.ToArray(),
                                    typeof(INetworkObjectProvider), so.FindProperty("objectProvider"), item.entry);
                                PopupWindow.Show(cellRect, typesPopup);
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
#if HAS_ADDRESSABLES
                            if (item.entry.Provider != null && CoherenceSyncConfigUtils.IsAddressable(item.entry) &&
                                !CoherenceSyncConfigUtils.ProviderIsAddressableOrCustom(item.entry.Provider))
                            {
                                typeName = new GUIContent(EditorGUIUtility.TrTextContentWithIcon(
                                    typeName.text,
                                    "Info"))
                                {
                                    tooltip =
                                        $"The entity is marked as 'Addressable' but the provider used to load it is '{typeName}' when it should be '{nameof(AddressablesProvider)}'.",
                                };
                            }
#endif
                            EditorGUI.LabelField(cellRect, typeName);
                        }
                    }
                    break;
                case ColumnId.InstantiateVia:
                    {
                        if (IsMissingAsset(item))
                        {
                            break;
                        }

                        var typeName = item.entry.Instantiator != null
                            ? INetworkObjectDrawer.typeDisplayNames[item.entry.Instantiator.GetType().FullName ?? string.Empty]
                            : new GUIContent("Missing");
                        if (EditMode)
                        {
                            if (EditorGUI.DropdownButton(cellRect, typeName, FocusType.Passive,
                                    CoherenceHubLayout.Styles.PopupNonFixedHeight))
                            {
                                var so = new SerializedObject(item.entry);
                                var typesPopup = new INetworkObjectDrawer.TypesPopup(objectInstantiatorTypes.ToArray(),
                                    typeof(INetworkObjectInstantiator), so.FindProperty("objectInstantiator"),
                                    item.entry);
                                PopupWindow.Show(cellRect, typesPopup);
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            EditorGUI.LabelField(cellRect, typeName);
                        }
                    }
                    break;
                case ColumnId.Simulate:
                    {
                        if (IsMissingAsset(item))
                        {
                            break;
                        }

                        if (serializedDataForItem.Sync != null)
                        {
                            if (EditMode)
                            {
                                CoherenceSyncEditor.DrawSimulateInternal(cellRect, serializedDataForItem.Sync,
                                    serializedDataForItem.SimulationProperty,
                                    serializedDataForItem.AuthorityTransferProperty, false);
                            }
                            else
                            {
                                EditorGUI.LabelField(cellRect,
                                    ((CoherenceSync.SimulationType)serializedDataForItem.SimulationProperty.intValue)
                                    .ToString());
                            }
                        }
                    }
                    break;
                case ColumnId.Lifetime:
                    {
                        if (IsMissingAsset(item))
                        {
                            break;
                        }

                        if (serializedDataForItem.Sync != null)
                        {
                            if (EditMode)
                            {
                                CoherenceSyncEditor.DrawLifetimeInternal(cellRect,
                                    serializedDataForItem.LifetimeProperty,
                                    serializedDataForItem.AuthorityTransferProperty, false);
                            }
                            else
                            {
                                EditorGUI.LabelField(cellRect,
                                    ((CoherenceSync.LifetimeType)serializedDataForItem.LifetimeProperty.intValue)
                                    .ToString());
                            }
                        }
                    }
                    break;
                case ColumnId.AuthorityTransfer:
                    {
                        if (IsMissingAsset(item))
                        {
                            break;
                        }

                        if (serializedDataForItem.Sync != null)
                        {
                            if (EditMode)
                            {
                                DrawTransferAuthority(cellRect, serializedDataForItem);
                            }
                            else
                            {
                                var displayName = CoherenceSyncEditor.GetAuthorityTransferDisplayName(
                                    (CoherenceSync.AuthorityTransferConfig)serializedDataForItem.AuthorityTransferProperty.intValue);
                                EditorGUI.LabelField(cellRect, displayName);
                            }
                        }
                    }
                    break;
            }
        }

        private bool IsInvalidEntry(NetworkObjectEntryTreeViewItem item)
        {
            return !analyzer.Validate(item.entry);
        }

        private static bool IsMissingAsset(NetworkObjectEntryTreeViewItem item)
        {
            return item.entry == null || item.entry.EditorTarget == null;
        }

        private string GetStatusTooltip(NetworkObjectEntryTreeViewItem item)
        {
            return analyzer.GetCompoundedErrorMessage(item.entry);
        }

        private void DrawTransferAuthority(Rect rect, SyncSerializedData data)
        {
            var sync = data.Sync;
            var isSimulationTypeServerSideWithClientInput =
                sync.simulationType == CoherenceSync.SimulationType.ServerSideWithClientInput;
            var isLifetimePersistent = sync.lifetimeType == CoherenceSync.LifetimeType.Persistent;

            EditorGUI.BeginDisabledGroup(Application.isPlaying || isSimulationTypeServerSideWithClientInput ||
                                         isLifetimePersistent);
            {
                CoherenceSyncEditor.DrawTransferAuthorityInternal(rect,
                                    data.AuthorityTransferProperty,
                                    data.LifetimeProperty.intValue == (int)CoherenceSync.LifetimeType.Persistent,
                                    useLabel: false);
            }

            EditorGUI.EndDisabledGroup();
        }

        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        private void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return;
            }

            SortByMultipleColumns();
            TreeToList(root, rows);
            Repaint();
        }

        private void SortByMultipleColumns()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            var myTypes = rootItem.children.Cast<NetworkObjectEntryTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch ((ColumnId)i)
                {
                    case ColumnId.Status:
                        orderedQuery = orderedQuery.ThenBy(OrderByStatus);
                        break;
                    case ColumnId.IncludeInSchema:
                        orderedQuery = orderedQuery.ThenBy(entry => entry.entry is { IncludeInSchema: true, }, ascending);
                        break;
                    case ColumnId.Name:
                        orderedQuery = orderedQuery.ThenBy(OrderByName, ascending);
                        break;
                    case ColumnId.LoadVia:
                        orderedQuery =
                            orderedQuery.ThenBy(entry => entry.entry is { } config ? config.Provider.GetType().Name : string.Empty,
                                ascending);
                        break;
                    case ColumnId.InstantiateVia:
                        orderedQuery =
                            orderedQuery.ThenBy(entry => entry.entry is { } config ? config.Instantiator.GetType().Name : string.Empty,
                                ascending);
                        break;
                    case ColumnId.Simulate:
                        orderedQuery = orderedQuery.ThenBy(OrderBySimProperty, ascending);
                        break;
                    case ColumnId.Lifetime:
                        orderedQuery = orderedQuery.ThenBy(OrderByLifeTimeProperty, ascending);
                        break;
                    case ColumnId.AuthorityTransfer:
                        orderedQuery = orderedQuery.ThenBy(OrderByAuthorityProperty, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        private int OrderByAuthorityProperty(NetworkObjectEntryTreeViewItem entry)
        {
            serializedData.TryGetValue(entry, out var data);
            return data.AuthorityTransferProperty?.intValue ?? 0;
        }

        private int OrderByLifeTimeProperty(NetworkObjectEntryTreeViewItem entry)
        {
            serializedData.TryGetValue(entry, out var data);
            return data.LifetimeProperty?.intValue ?? 0;
        }

        private int OrderBySimProperty(NetworkObjectEntryTreeViewItem entry)
        {
            serializedData.TryGetValue(entry, out var data);
            return data.SimulationProperty?.intValue ?? 0;
        }

        private string OrderByName(NetworkObjectEntryTreeViewItem entry)
        {
            return entry.displayName;
        }

        private IOrderedEnumerable<NetworkObjectEntryTreeViewItem> InitialOrder(
            IEnumerable<NetworkObjectEntryTreeViewItem> myTypes, int[] history)
        {
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch ((ColumnId)history[0])
            {
                case ColumnId.Status:
                    return myTypes.Order(OrderByStatus, ascending);
                case ColumnId.IncludeInSchema:
                    return myTypes.Order(entry => entry.entry is { IncludeInSchema: true, }, ascending);
                case ColumnId.Name:
                    return myTypes.Order(OrderByName, ascending);
                case ColumnId.LoadVia:
                    return myTypes.Order(entry => entry.entry is { } config ? config.Provider.GetType().Name : string.Empty, ascending);
                case ColumnId.InstantiateVia:
                    return myTypes.Order(entry => entry.entry is { } config ? config.Instantiator.GetType().Name : string.Empty, ascending);
                case ColumnId.Simulate:
                    return myTypes.Order(OrderBySimProperty, ascending);
                case ColumnId.Lifetime:
                    return myTypes.Order(OrderByLifeTimeProperty, ascending);
                case ColumnId.AuthorityTransfer:
                    return myTypes.Order(OrderByAuthorityProperty, ascending);
            }

            // default
            return myTypes.Order(entry => entry.entry is { } config ? config.EditorTarget.name : string.Empty, ascending);
        }

        private int OrderByStatus(NetworkObjectEntryTreeViewItem entry)
        {
            return IsInvalidEntry(entry) ? 1 : 0;
        }

        private static void TreeToList(TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
            {
                throw new NullReferenceException("root");
            }

            if (result == null)
            {
                throw new NullReferenceException("result");
            }

            result.Clear();

            if (root.children == null)
            {
                return;
            }

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            for (int i = root.children.Count - 1; i >= 0; i--)
            {
                stack.Push(root.children[i]);
            }

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                result.Add(current);

                if (current.hasChildren && current.children[0] != null)
                {
                    for (int i = current.children.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.children[i]);
                    }
                }
            }
        }

        private void OpenOptimizeWindow(NetworkObjectEntryTreeViewItem item)
        {
            SelectAndPingObject(item.entry.EditorTarget);
            BindingsWindow.Init();
        }

        private void OpenConfigureWindow(NetworkObjectEntryTreeViewItem item)
        {
            SelectAndPingObject(item.entry.EditorTarget);
            var w = CoherenceSyncBindingsWindow.GetWindow();
            w.scope = CoherenceSyncBindingsWindow.Scope.Variables;
        }

        protected override void ContextClickedItem(int id)
        {
            List<NetworkObjectEntryTreeViewItem> selectedNodes = new List<NetworkObjectEntryTreeViewItem>();
            foreach (var nodeId in GetSelection())
            {
                var item = FindItem(nodeId, rootItem) as NetworkObjectEntryTreeViewItem;
                if (item != null)
                {
                    selectedNodes.Add(item);
                }
            }

            if (selectedNodes.Count == 0)
                return;

            bool selectedEntry = false;

            foreach (var item in selectedNodes)
            {
                if (item.entry != null)
                {
                    selectedEntry = true;
                }
            }

            var menu = new GenericMenu();

            if (selectedEntry)
            {
                if (selectedNodes.Count == 1)
                {
                    var config = selectedNodes[0].entry;

                    if (config)
                    {
                        if (!CloneMode.Enabled && CoherenceSyncConfigUtils.CanLink(config))
                        {
                            menu.AddItem(new GUIContent("Fix: Link with CoherenceSync"), false, () =>
                            {
                                if (CoherenceSyncConfigUtils.Link(config))
                                {
                                    Reload();
                                }
                            });
                        }
                        var sync = config.Sync;

                        analyzer.TryGetInfo(config, out var info);

                        if (info.InvalidBindings > 0 && !CloneMode.Enabled)
                        {
                            menu.AddItem(new GUIContent("Fix: Remove Invalid Bindings"), false,
                                () => CoherenceSyncUtils.RemoveInvalidBindings(sync));
                            menu.AddSeparator(string.Empty);
                        }

                        menu.AddItem(new GUIContent("Configure"), false,
                            () => OpenConfigureWindow(selectedNodes[0]));
                        menu.AddItem(new GUIContent("Optimize"), false,
                            () => OpenOptimizeWindow(selectedNodes[0]));
                        menu.AddSeparator(string.Empty);
                    }
                    else
                    {
                        if (!CloneMode.Enabled)
                        {
                            menu.AddItem(new GUIContent("Fix: Reimport Registry"), false, () => CoherenceSyncConfigRegistry.Instance.ReimportConfigs());
                        }
                    }
                }

                if (CloneMode.Enabled)
                {
                    menu.AddDisabledItem(EditorGUIUtility.TrTempContent("Remove from Network"));
                }
                else
                {
                    menu.AddItem(EditorGUIUtility.TrTempContent("Remove from Network"), false,
                        DeleteSelectedEntries(selectedNodes));
                }
            }

            menu.ShowAsContext();
        }

        private GenericMenu.MenuFunction DeleteSelectedEntries(List<NetworkObjectEntryTreeViewItem> selectedNodes)
        {
            return () =>
            {
                foreach (var item in selectedNodes)
                {
                    CoherenceSyncConfigUtils.Delete(item.entry);
                }

                Reload();
            };
        }

        private void SelectAndPingObject(Object obj)
        {
            selectionOriginatedFromTreeView = true;
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        protected override void DoubleClickedItem(int id)
        {
            if (FindItem(id, rootItem) is NetworkObjectEntryTreeViewItem item)
            {
                OpenConfigureWindow(item);
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            foreach (var id in args.draggedItemIDs)
            {
                var item = FindItem(id, rootItem) as NetworkObjectEntryTreeViewItem;

                if (item == null || item.entry == null)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var selectedNodes = new List<NetworkObjectEntryTreeViewItem>();
            foreach (var id in args.draggedItemIDs)
            {
                var item = FindItem(id, rootItem) as NetworkObjectEntryTreeViewItem;
                if (item != null && item.entry != null)
                {
                    selectedNodes.Add(item);
                }
            }

            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = new Object[]
            {
            };
            DragAndDrop.SetGenericData("NetworkObjectEntryTreeViewItem", selectedNodes);
            DragAndDrop.visualMode =
                selectedNodes.Count > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            DragAndDrop.StartDrag("NetworkObjectsTree");
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            var alt = Event.current.alt;
            var selectedObjects = new Object[selectedIds.Count];
            for (var i = 0; i < selectedIds.Count; i++)
            {
                if (FindItem(selectedIds[i], rootItem) is not NetworkObjectEntryTreeViewItem item)
                {
                    continue;
                }

                if (item.entry)
                {
                    selectedObjects[i] = alt ? item.entry.EditorTarget : item.entry;
                }
            }

            selectionOriginatedFromTreeView = true;
            Selection.objects = selectedObjects;
        }

        enum ColumnId
        {
            Status,
            IncludeInSchema,
            Name,
            ConfigName,
            LoadVia,
            InstantiateVia,
            Simulate,
            Lifetime,
            AuthorityTransfer
        }

        private class NetworkObjectEntryTreeViewItem : TreeViewItem
        {
            public CoherenceSyncConfig entry;

            public NetworkObjectEntryTreeViewItem(CoherenceSyncConfig config, int depth) : base(
                config ? config.ID.GetHashCode() : 0,
                depth,
                config && config.EditorTarget
                    ? config.EditorTarget.name
                    : config ? config.name : string.Empty)
            {
                entry = config;
            }
        }
    }

    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector,
            bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }

            return source.OrderByDescending(selector);
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector,
            bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }

            return source.ThenByDescending(selector);
        }
    }
}
