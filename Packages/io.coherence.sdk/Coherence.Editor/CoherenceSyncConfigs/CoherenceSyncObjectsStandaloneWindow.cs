// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    public class CoherenceSyncObjectsStandaloneWindow : EditorWindow
    {
        [SerializeField]
        private TreeViewState treeState;

        [SerializeField]
        MultiColumnHeaderState multiColumnHeaderState;

        private CoherenceSyncObjectsTreeView treeView;

        private ConfigsAnalyzerHandler analyzer;

        private bool gatheredInfo;

        private SearchField searchField;
        private string searchText = string.Empty;
        private string newSearchText = string.Empty;
        private Vector2 scrollPosition;

        private static class GUIContents
        {
            public static readonly GUIContent title = Icons.GetContentWithText(
                "EditorWindow",
                "Networked Prefabs");

            public static readonly GUIContent editMode = new("Edit Mode");
            public static readonly GUIContent empty = new(
                $"No {nameof(CoherenceSync)} Prefabs registered.\n\n" +
                "To start networking a Prefab, select it and tick the " +
                "\"Sync with coherence\" checkbox in the Inspector Window.\n\n" +
                $"If you already have Prefabs using {nameof(CoherenceSync)} but they are not listed here, " +
                "click on Reimport to get them loaded.");
            public static readonly GUIContent controls = new(
                $"Click selects the Config.\n" +
                $"{GetOsSpecificModifier()}-Click selects the Prefab.\n" +
                "Double-Click opens the Configure Window for that Prefab.");

            private static string GetOsSpecificModifier() =>
                SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX ? "Option" : "Alt";
        }

        public static void Open()
        {
            _ = GetWindow<CoherenceSyncObjectsStandaloneWindow>();
        }

        private void OnEnable()
        {
            titleContent = GUIContents.title;
            Init();
        }

        private void OnDisable()
        {
            treeView.OnDisable();
        }

        internal void OnGUI()
        {
            Init();

            CoherenceHeader.OnSlimHeader(string.Empty);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.BeginHorizontal(CoherenceHubLayout.Styles.HorizontalMargins);

            EditorGUI.BeginChangeCheck();
            searchText = ContentUtils.DrawSearchField(searchText, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                treeView.searchString = searchText;
            }

            EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
            treeView.EditMode = GUILayout.Toggle(treeView.EditMode,
                GUIContents.editMode,
                ContentUtils.GUIStyles.toolbarButton);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            ContentUtils.DrawCloneModeMessage();

            using var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition);
            scrollPosition = scroll.scrollPosition;
            using (new EditorGUILayout.VerticalScope(CoherenceHubLayout.Styles.HorizontalMargins))
            {
                var registry = CoherenceSyncConfigRegistry.Instance;
                if (registry.Count == 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(GUIContents.empty, ContentUtils.GUIStyles.centeredStretchedLabel);
                        GUILayout.FlexibleSpace();
                    }

                    return;
                }

                EditorGUILayout.Separator();

                NetworkObjectsInfoDrawer.DrawInvalidBindings(analyzer.AllEntriesInfo.InvalidBindings, OnRefresh);
                NetworkObjectsInfoDrawer.DrawMissingFromPreloadedAssets();
                if (CoherenceSyncConfigUtils.AnyIssues)
                {
                    if (GUILayout.Button("Delete Affected Configs"))
                    {
                        CoherenceSyncConfigUtils.DeleteConfigsWithIssues();
                    }
                }

                EditorGUILayout.Separator();
            }

            using (var scope = new EditorGUILayout.VerticalScope(CoherenceHubLayout.Styles.HorizontalMargins,
                       GUILayout.ExpandHeight(true)))
            {
                treeView.OnGUI(scope.rect);
            }

            using (new EditorGUILayout.VerticalScope(CoherenceHubLayout.Styles.HorizontalMargins))
            {
                GUILayout.Label(GUIContents.controls, ContentUtils.GUIStyles.miniLabelGreyWrap);
            }

            EditorGUILayout.Space();
        }

        private void OnRefresh()
        {
            gatheredInfo = false;
            GetNetworkedInfo();
            treeView.Reload();
        }

        private void Init()
        {
            if (searchField == null)
            {
                searchField = new SearchField();
            }

            if (treeState == null)
            {
                treeState = new TreeViewState();
            }

            var headerState = CoherenceSyncObjectsTreeView.CreateDefaultMultiColumnHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
            multiColumnHeaderState = headerState;

            if (treeView == null)
            {
                analyzer = new ConfigsAnalyzerHandler();
                GetNetworkedInfo();
                treeView = new CoherenceSyncObjectsTreeView(treeState, new MultiColumnHeader(multiColumnHeaderState),
                    analyzer);
                treeView.Reload();
                treeView.multiColumnHeader.ResizeToFit();
            }
        }

        private void GetNetworkedInfo()
        {
            if (!gatheredInfo)
            {
                analyzer.RefreshConfigsInfo();
                gatheredInfo = true;
            }
        }
    }
}
