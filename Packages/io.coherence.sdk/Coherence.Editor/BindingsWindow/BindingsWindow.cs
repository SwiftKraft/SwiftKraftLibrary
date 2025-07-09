// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Archetypes;
    using UnityEditor.IMGUI.Controls;

    /// <summary>
    /// The Optimization window.
    /// </summary>
    internal class BindingsWindow : InspectComponentWindow<CoherenceSync>
    {
        internal static bool EditingAllFields = false;

        internal BindingsWindowToolbar Toolbar { get; private set; }
        internal BindingsWindowTree Tree { get; private set; }
        internal BindingsWindowTreeHeader TreeHeader { get; private set; }
        internal BindingsWindowStateController StateController { get; private set; }

        [SerializeField] private TreeViewState treeViewState;
        [SerializeField] private MultiColumnHeaderState headerState;

        private int lastComponentCount;
        private WarningToken warning;

        public static void Init() => GetWindow<BindingsWindow>();

        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateEventSubscriptions();

            minSize = BindingsWindowSettings.MinSize;
            titleContent = BindingsWindowSettings.WindowTitle;

            if (Toolbar == null)
            {
                Toolbar = new BindingsWindowToolbar(this);
            }

            CreateNewTreeView();
            RefreshTree(true);
            Repaint();

            EditorApplication.projectChanged += OnFocus;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.projectChanged -= OnFocus;
            BindingsWindowSettings.ShowStatistics.Changed -= OnShowStatisticsSettingChanged;
            BindingsWindowSettings.AutoSave.Changed -= OnAutoSaveChanged;
            BindingsWindowSettings.All.RemoveChangedListener(OnAnySettingChanged);
            Tree?.Dispose();
            Tree = null;
        }

        protected override void OnFocus()
        {
            base.OnFocus();
            RefreshTree(true);
            Repaint();
        }

        internal void CreateNewTreeView(bool clearTree = false)
        {
            if (StateController == null)
            {
                StateController = new BindingsWindowStateController();
            }

            if (treeViewState == null || clearTree)
            {
                treeViewState = new TreeViewState();
            }

            if (Component)
            {
                Component.ValidateArchetype();

                StateController.SetObject(Component);
                headerState = BindingsWindowTreeHeader.CreateColumns(Component, StateController.CurrentState);
                TreeHeader = new BindingsWindowTreeHeader(this, Component, headerState);

                Tree?.Dispose();
                Tree = new BindingsWindowTree(treeViewState, this, TreeHeader);
            }
        }

        internal void StateChanged()
        {
            CreateNewTreeView();
        }

        protected override void OnUndoRedo()
        {
            base.OnUndoRedo();
            CreateNewTreeView(true);
        }

        internal void SetEditingAllFields(bool editingAllFields)
        {
            EditingAllFields = editingAllFields;
            Tree.UpdateOnly();
        }

        public override void Refresh(CoherenceSync component)
        {
            base.Refresh(component);

            lastComponentCount = Component ? Component.Archetype.CachedComponents.Count : 0;
            RefreshTree(true);
            if (Component)
            {
                if (Component.Archetype.CachedComponents != null && Component.Archetype.CachedComponents.Count != lastComponentCount)
                {
                    CreateNewTreeView(true);
                }
            }
            Repaint();
            ClearWarning();
        }

        internal void RefreshTree(bool reloadTree)
        {
            if (Tree != null)
            {
                if (Tree.Sync != Component)
                {
                    CreateNewTreeView();
                }
                else
                {
                    if (reloadTree)
                    {
                        Tree.Reload();
                    }
                    Tree.Repaint();
                }
            }
            Repaint();
        }

        protected override void OnActiveSelectionChanging(CoherenceSync currentComponent, CoherenceSync nextComponent)
        {
            if (!currentComponent || ReferenceEquals(currentComponent, nextComponent))
            {
                return;
            }

            if (!hasUnsavedChanges)
            {
                SaveHasUnsavedChanges(currentComponent, false);
                return;
            }

            const int Save = 0;
            const int Discard = 2;
            switch (EditorUtility.DisplayDialogComplex("Save Changes To Disk?", "Would you like to apply all unsaved changes to the prefab asset now?", ok:"Save", cancel:"Don't Save", alt:"Discard"))
            {
                case Save:
                    SaveChanges();
                    return;
                case Discard:
                    DiscardChanges();
                    return;
                default:
                    SaveHasUnsavedChanges(currentComponent, hasUnsavedChanges);
                    return;
            }
        }

        protected override void OnActiveSelectionChanged(CoherenceSync previousComponent, CoherenceSync newComponent)
        {
            base.OnActiveSelectionChanged(previousComponent, newComponent);
            RefreshTree(true);
            if (newComponent && !ReferenceEquals(previousComponent, newComponent))
            {
                hasUnsavedChanges = GetHasUnsavedChanges(newComponent);
            }
        }

        public override void SaveChanges()
        {
            if (component)
            {
                SaveHasUnsavedChanges(component, false);
            }

            BindingsWindowSettings.All.Save();
            UpdateSerialization();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            if (component)
            {
                SaveHasUnsavedChanges(component, false);
            }

            BindingsWindowSettings.All.Discard();
            if (Component && AssetDatabase.GetAssetPath(Component) is { Length: > 0 } prefabPath)
            {
                AssetDatabase.ForceReserializeAssets(new[] { prefabPath }, ForceReserializeAssetsOptions.ReserializeAssets);
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
            }

            Tree?.Dispose();
            CreateNewTreeView(true);
            base.DiscardChanges();
        }

        /// <summary>
        /// Applies all modifications done to the target to disk.
        /// </summary>
        internal void UpdateSerialization()
        {
            if (!Component)
            {
                return;
            }

            using (var newSerializedObject = new SerializedObject(Component))
            {
                _ = newSerializedObject.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(Component);
            Repaint();
            this.serializedObject.Update();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (Toolbar == null)
            {
                Toolbar = new BindingsWindowToolbar(this);
            }

            if (Component)
            {
                Toolbar.DrawToolbar();
                ContentUtils.DrawCloneModeMessage();
                DrawHeader();

                EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
                DrawTree();
                if (StateController.Lods)
                {
                    var lodsRect = EditorGUILayout.GetControlRect(GUILayout.Height(BindingsWindowSettings.LodFooterHeight));
                    DrawBandwidthFooter(lodsRect);
                }

                Toolbar.DrawFooter(this);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.LabelField("Select a Game Object with a CoherenceSync component attached.", ContentUtils.GUIStyles.centeredStretchedLabel);
            }

        }

        private void DrawTree()
        {
            Rect treeRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            if (Component)
            {
                if (Tree == null)
                {
                    CreateNewTreeView();
                }

                Tree.OnGUI(treeRect);
            }
        }

        private class GUIContents
        {
            public static readonly GUIContent selectAll = Icons.GetContent("Coherence.Select.All.Off", "Select All");
            public static readonly GUIContent selectAllHover = Icons.GetContent("Coherence.Select.All.On", "Select All");
            public static readonly GUIContent deselectAll = Icons.GetContent("Coherence.Select.None.Off", "Deselect All");
            public static readonly GUIContent deselectAllHover = Icons.GetContent("Coherence.Select.None.On", "Deselect All");
            public static readonly GUIContent experimental = EditorGUIUtility.TrTextContent("Experimental: this coherence window contains features that may not have full functionality or polished design.");
            public static readonly GUIContent experimentalIcon =  Icons.GetContent("Coherence.Message.Experimental", "experimental");
        }

        private class GUIStyles
        {
            public static readonly GUIStyle MessageBox = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(6, 6, 8, 8)
            };

            public static readonly GUIStyle MessageIcon = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(8, 5, 8, 5),
                stretchWidth = false
            };

            public static readonly GUIStyle MessageText = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 8, 6),
                wordWrap = true
            };

            public static readonly GUIStyle Hint = new GUIStyle(ContentUtils.GUIStyles.centeredGreyMiniLabelWrap)
            {
                margin = new RectOffset(0, 0, 0, 3)
            };

            public static readonly GUIStyle hoverButton = new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private int hoverControl;

        internal bool DrawSelectAllButton(Rect rect)
        {
            return HoverButton(rect, GUIContents.selectAll, GUIContents.selectAllHover);

        }
        internal bool DrawSelectNoneButton(Rect rect)
        {
            return HoverButton(rect, GUIContents.deselectAll, GUIContents.deselectAllHover);
        }

        private bool HoverButton(Rect rect, GUIContent normalContent, GUIContent hoverContent)
        {
            var style = GUIStyles.hoverButton;
            var hover = rect.Contains(Event.current.mousePosition);
            var c = hover ? hoverContent : normalContent;
            int id = GUIUtility.GetControlID(c, FocusType.Passive);
            if (Event.current.type == EventType.MouseMove)
            {
                if (hover)
                {
                    if (hoverControl != id)
                    {
                        hoverControl = id;

                        if (mouseOverWindow)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
                else
                {
                    if (hoverControl == id)
                    {
                        hoverControl = 0;

                        if (mouseOverWindow)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
            }
            return GUI.Button(rect, c, style);
        }

        private void DrawBandwidthFooter(Rect rect)
        {
            int labelsWidth = 50;
            int lodWidth = 60;
            int addLodButtonWidth = 60;
            int settingsWidth = 200;
            int lodCount = Component.Archetype.LODLevels.Count;

            GUIStyle header = new GUIStyle(EditorStyles.boldLabel);
            header.fontSize = 14;
            header.alignment = TextAnchor.MiddleLeft;

            DrawLODLabels(new Rect(rect.x, rect.y, labelsWidth, rect.height));

            for (int i = 0; i < lodCount; i++)
            {
                Rect lodRect = new Rect(rect.x + labelsWidth + (i * lodWidth), rect.y, lodWidth - 1, rect.height);
                DrawLODButton(lodRect, header, i);
            }

            Rect addRect = new Rect(rect.x + labelsWidth + lodCount * lodWidth, rect.y, addLodButtonWidth, rect.height);
            DrawAddLODButton(addRect);

            Rect settingsRect = new Rect(addRect.xMax + 2, rect.y, settingsWidth, rect.height);
            // BindingsWindowSettings.DrawSettings(settingsRect, this);
        }
        private void DrawLODLabels(Rect rect)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.alignment = TextAnchor.MiddleRight;

            Rect topRect = new Rect(rect.x, rect.y, rect.width - 2, rect.height / 3);
            Rect middle = new Rect(rect.x, topRect.yMax, rect.width - 2, rect.height / 3);
            Rect bottom = new Rect(rect.x, middle.yMax, rect.width - 2, rect.height / 3);

            GUI.Label(topRect, "LOD", style);
            GUI.Label(middle, "Bits", style);
            GUI.Label(bottom, "Distance", style);
        }

        private void DrawLODButton(Rect rect, GUIStyle headerStyle, int lodStep)
        {
            bool baseStep = lodStep == 0;
            //Color color = CoherenceArchetypeDrawer.GetLODColor((float) lodStep / Sync.Archetype.LODLevels.Count) * .5f;
            Color color = BindingsWindowSettings.HeaderColor;
            EditorGUI.DrawRect(rect, color);

            int buttonSize = 14;

            // Rects
            Rect topRect = new Rect(rect.x, rect.y, rect.width, rect.height / 3);
            Rect middle = new Rect(rect.x, topRect.yMax, rect.width, rect.height / 3);
            Rect bottom = new Rect(rect.x, middle.yMax, rect.width, rect.height / 3);

            Rect labelRect = new Rect(topRect.x, topRect.y, lodStep == 0 ? topRect.width : topRect.width - buttonSize, topRect.height);
            Rect deleteRect = new Rect(topRect.xMax - buttonSize, topRect.y, buttonSize, topRect.height);

            // Get Data
            ArchetypeLODStep LodLevel = Component.Archetype.LODLevels[lodStep];
            int baseBits = Component.Archetype.GetTotalActiveBitsOfLOD(0);
            int bits = Component.Archetype.GetTotalActiveBitsOfLOD(lodStep);
            float percentage = (float)bits / baseBits * 100;
            float distance = LodLevel.Distance;

            // Label
            //headerStyle.alignment = TextAnchor.MiddleLeft;
            string label = lodStep == 0 ? "Base" : $"LOD {lodStep}";
            //GUI.Label(labelRect, label, headerStyle);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.richText = true;
            labelStyle.alignment = TextAnchor.MiddleLeft;

            //GUI.Label(labelRect, label, labelStyle);
            if (GUI.Button(labelRect, label, labelStyle))
            {
                Tree.FocusOnLOD(lodStep);
            }

            // Bits
            //headerStyle.alignment = TextAnchor.MiddleRight;
            //GUI.Label(middle, $"{bits} {(bits == 1 ? "Bit" : "Bits")}");
            GUI.Label(middle, $"<b>{bits}</b><size=9> ({percentage.ToString("N0")}%)</size>", labelStyle);

            if (!baseStep)
            {
                /*
                bottom.width = rect.width *.5f;
                bottom.x -= bottom.width *.5f;
                */
                // Distance field
                EditorGUI.BeginChangeCheck();
                float newDistance = EditorGUI.DelayedFloatField(bottom, GUIContent.none, distance, labelStyle);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Component, "Set Distance on Lodstep");
                    SetLODDistance(Component.Archetype, lodStep, newDistance);
                    OnPropertyValueChanged();
                }

                // Delete Button
                GUIContent delete = EditorGUIUtility.IconContent("P4_DeletedLocal", "Delete");
                if (GUI.Button(deleteRect, delete, GUI.skin.label))
                {
                    Undo.RecordObject(Component, "Removed LOD");
                    Component.Archetype.RemoveLodLevel(lodStep);
                    StateController.RemoveLOD(lodStep);
                    CreateNewTreeView(true);
                    OnPropertyValueChanged();
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                GUI.Label(bottom, $"{distance}", labelStyle);
            }
        }

        private void DrawAddLODButton(Rect rect)
        {
            Rect buttonRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUIContent add = new GUIContent("Add LOD", "Add Level of Detail");
            if (GUI.Button(buttonRect, add))
            {
                Undo.RecordObject(Component, "Added LOD");
                Component.Archetype.AddLODLevel(true);
                CreateNewTreeView(true);
                OnPropertyValueChanged();
            }
        }

        public virtual void OnPropertyValueChanged()
        {
            if (BindingsWindowSettings.AutoSave || !IsComponentPrefab())
            {
                SaveChanges();
                RefreshTree(true);
            }
            else
            {
                hasUnsavedChanges = true;
                if (component)
                {
                    SaveHasUnsavedChanges(component, true);
                }
            }
        }

        private void DrawWarning()
        {
            if (CoherenceHubLayout.DrawWarnArea(warning.Message, dismissible: true))
            {
                ClearWarning();
            }
        }
        private void DrawHeader()
        {
            DrawExperimentalDisclaimer();

		    if (HasWarning())
            {
                DrawWarning();
            }

            GUILayout.Label(BindingsWindowSettings.Hint, GUIStyles.Hint);
        }

        private static void DrawExperimentalDisclaimer()
        {
            using (new EditorGUILayout.HorizontalScope(GUIStyles.MessageBox))
            {
                GUILayout.Label(GUIContents.experimentalIcon, GUIStyles.MessageIcon, GUILayout.ExpandWidth(false));

                EditorGUILayout.LabelField(GUIContents.experimental, GUIStyles.MessageText);
            }
        }

        internal void SetWarning(WarningToken message)
        {
            warning = message;
        }

        internal void ClearWarning()
        {
            warning?.ClearWarning();
            warning = null;
        }

        private bool HasWarning()
        {
            return warning?.Active ?? false;
        }

        internal void SetLODDistance(ToolkitArchetype archetype, int lodStep, float distance)
        {
            for (int i = 0; i < archetype.LODLevels.Count; i++)
            {
                var lodLevel = archetype.LODLevels[i];
                if (i == 0)
                {
                    lodLevel.SetDistance(0);
                }
                else if (i == lodStep)
                {
                    lodLevel.SetDistance(distance);
                }
                else
                {
                    float current = lodLevel.Distance;
                    float newDistance = i >= lodStep ? Mathf.Max(distance, current) : Mathf.Min(distance, current);
                    lodLevel.SetDistance(newDistance);
                }
            }
        }

        private void UpdateEventSubscriptions()
        {
            BindingsWindowSettings.ShowStatistics.Changed -= OnShowStatisticsSettingChanged;
            BindingsWindowSettings.ShowStatistics.Changed += OnShowStatisticsSettingChanged;
            BindingsWindowSettings.AutoSave.Changed -= OnAutoSaveChanged;
            BindingsWindowSettings.AutoSave.Changed += OnAutoSaveChanged;
            BindingsWindowSettings.All.RemoveChangedListener(OnAnySettingChanged);
            BindingsWindowSettings.All.AddChangedListener(OnAnySettingChanged);
        }

        private void OnAutoSaveChanged(bool autoSave)
        {
            autoSave |= IsComponentPrefab();
            BindingsWindowSettings.All.SetAutoSave(autoSave);

            if (autoSave)
            {
                SaveChanges();
                RefreshTree(true);
            }
        }

        private void OnAnySettingChanged(bool boolean) => RefreshTree(false);
        private void OnShowStatisticsSettingChanged(bool showStatistics) => TreeHeader.SetStatisticsVisible(showStatistics);

        private static void SaveHasUnsavedChanges(Component component, bool hasUnsavedChanges)
        {
            if (GetHasUnsavedChangesKey(component) is { Length: > 0 } key)
            {
                SessionState.SetBool(key, hasUnsavedChanges);
            }
        }

        private static bool GetHasUnsavedChanges(Component component) => GetHasUnsavedChangesKey(component) is { Length: > 0 } key && SessionState.GetBool(key, false);
        private static string GetHasUnsavedChangesKey(Component component) => AssetDatabase.GetAssetPath(component) is { Length: > 0 } assetPath ? "HasUnsavedChanges:" + assetPath : "";
        private bool IsComponentPrefab() => component && PrefabUtility.IsPartOfPrefabAsset(component);
    }
}
