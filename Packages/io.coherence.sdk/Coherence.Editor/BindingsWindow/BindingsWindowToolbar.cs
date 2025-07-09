// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;
    using UnityEditor.SceneManagement;

    internal class BindingsWindowToolbar
    {
        private BindingsWindow editingWindow;

        private int toolbarHeight;
        private int footerHeight;
        internal BindingsWindowTreeFilters Filters { private set; get; }
        private GUIStyle darkVerticalStyle;
        private static readonly GUILayoutOption[] autoSaveGuiLayoutOptions = { GUILayout.Width(85f), GUILayout.ExpandWidth(false) };

        internal BindingsWindowToolbar(BindingsWindow syncEditingWindow)
        {
            editingWindow = syncEditingWindow;
            toolbarHeight = BindingsWindowSettings.ToolbarHeight;
            footerHeight = BindingsWindowSettings.FooterHieght;
            Filters = new BindingsWindowTreeFilters(syncEditingWindow);

            darkVerticalStyle = UIHelpers.BackgroundStyle.Get("WindowBackground");
        }

        internal void DrawToolbar()
        {
            CoherenceSync sync = editingWindow.Component;
            _ = EditorGUILayout.BeginVertical(darkVerticalStyle);
            _ = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(toolbarHeight));

            DrawSync(sync);

            GUILayout.FlexibleSpace();

            Filters.DrawFilters();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        internal void DrawFooter(BindingsWindow bindingsWindow) {
            _ = EditorGUILayout.BeginHorizontal(GUILayout.Height(footerHeight));
            GUILayout.Space(5);
            if (editingWindow.StateController.Lods)
            {
                BindingsWindowSettings.DrawSettings();
            }
            GUILayout.FlexibleSpace();

            if (bindingsWindow.Component && PrefabUtility.IsPartOfPrefabAsset(bindingsWindow.Component))
            {
                GUILayout.Space(5);

                var saveContent = new GUIContent("Save", "Apply all unsaved changes to the Prefab Asset.");
                if (bindingsWindow.hasUnsavedChanges && !BindingsWindowSettings.AutoSave && GUILayout.Button(saveContent, ContentUtils.GUIStyles.saveButton))
                {
                    bindingsWindow.SaveChanges();
                }

                var autoSaveContent = new GUIContent("Auto Save", "When Auto Save is enabled, every change you make is automatically saved to the Prefab Asset. Disable Auto Save if you experience long import times.");
                BindingsWindowSettings.AutoSave.Value = EditorGUILayout.ToggleLeft(autoSaveContent, BindingsWindowSettings.AutoSave, autoSaveGuiLayoutOptions);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSync(CoherenceSync sync) {
            if (sync)
            {
                bool inStage = StageUtility.GetCurrentStage() != StageUtility.GetMainStage();
                bool isAsset = PrefabUtility.IsPartOfPrefabAsset(sync) || inStage;
                GUIStyle titleButton = new GUIStyle(EditorStyles.toolbarButton);
                titleButton.alignment = TextAnchor.MiddleLeft;

                var icon = isAsset ? AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(sync)) : PrefabUtility.GetIconForGameObject(sync.gameObject);
                var content = EditorGUIUtility.TrTextContentWithIcon(sync.name, icon);
                var width = titleButton.CalcSize(new GUIContent(sync.name)).x + 20f;
                if (GUILayout.Button(content, titleButton, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(width)))
                {
                    Selection.activeObject = sync.gameObject;
                    if (AssetDatabase.Contains(sync.gameObject))
                    {
                        EditorGUIUtility.PingObject(sync.gameObject);
                    }
                }
            }
        }
    }
}
