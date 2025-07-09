// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(CoherenceSyncConfigRegistry))]
    internal class CoherenceSyncConfigRegistryEditor : PreloadedSingletonEditor
    {
        private CoherenceSyncConfigRegistry registry;
        private Dictionary<CoherenceSyncConfig, GUIContent> cachedContent = new();
        private ConfigsAnalyzerHandler analyzer;
        private string searchText = string.Empty;

        private static class GUIContents
        {
            public static readonly GUIContent openInWindow = EditorGUIUtility.TrTextContentWithIcon("Open Networked Prefabs Window", "Prefab Icon");
            public static readonly GUIContent hasSubassets = EditorGUIUtility.TrTextContentWithIcon($"{nameof(CoherenceSyncConfig)}s as subassets is now deprecated and won't be supported in upcoming versions. Please extract them.", "Warning");
            public static readonly GUIContent extractSubassets = new("Extract All SubAssets");
            public static readonly GUIContent hasIssues = EditorGUIUtility.TrTextContentWithIcon($"Found {nameof(CoherenceSyncConfig)}s that aren't properly linked or registered.", "Warning");
            public static readonly GUIContent deleteWIthIssues = new("Delete Affected Configs");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UseMargins = false;
            registry = target as CoherenceSyncConfigRegistry;
            Reload();
            analyzer = new ConfigsAnalyzerHandler();
        }

        private void Reload()
        {
            analyzer = new ConfigsAnalyzerHandler();
        }

        internal void OnMainGUI()
        {
            if (GUI.enabled)
            {
                if (GUILayout.Button(GUIContents.openInWindow, ContentUtils.GUIStyles.bigButton))
                {
                    CoherenceSyncObjectsStandaloneWindow.Open();
                }
            }

            if (CoherenceSyncConfigUtils.AnySubassets)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(GUIContents.hasSubassets, ContentUtils.GUIStyles.miniLabel);
                if (GUILayout.Button(GUIContents.extractSubassets, ContentUtils.GUIStyles.bigButton))
                {
                    CoherenceSyncConfigUtils.ExtractSubAssets(registry);
                }
                EditorGUILayout.EndVertical();
            }

            if (CoherenceSyncConfigUtils.AnyIssues)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(GUIContents.hasIssues, ContentUtils.GUIStyles.miniLabel);
                if (GUILayout.Button(GUIContents.deleteWIthIssues, ContentUtils.GUIStyles.bigButton))
                {
                    CoherenceSyncConfigUtils.DeleteConfigsWithIssues();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            DrawConfigs();
        }

        protected override void OnGUI()
        {
            using var scope = new EditorGUILayout.VerticalScope(EditorStyles.inspectorDefaultMargins);

            OnMainGUI();
        }

        private void DrawConfigs()
        {
            EditorGUI.BeginChangeCheck();
            searchText = ContentUtils.DrawSearchField(searchText);
            if (EditorGUI.EndChangeCheck())
            {
                analyzer.RefreshConfigsInfo();
            }

            var leakedCount = registry.LeakedCount;
            for (var i = 0; i < leakedCount; i++)
            {
                var config = registry.GetLeakedAt(i);
                if (config.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    continue;
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(config, typeof(CoherenceSyncConfig), false);
                EditorGUI.EndDisabledGroup();

                DrawWarning(config);
            }

            var count = registry.Count;
            var shownCount = 0;

            if (count > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    var config = registry.GetAt(i);

                    if (!config)
                    {
                        continue;
                    }

                    var searchTarget = config.EditorTarget ? config.EditorTarget : config;
                    if (searchTarget.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        continue;
                    }

                    shownCount++;

                    EditorGUI.BeginDisabledGroup(true);
                    if (config.EditorTarget)
                    {
                        EditorGUILayout.ObjectField(config.EditorTarget, typeof(GameObject), false);
                    }
                    else
                    {
                        EditorGUILayout.ObjectField(config, typeof(CoherenceSyncConfig), false);
                    }
                    EditorGUI.EndDisabledGroup();

                    if (!config.IsLinked)
                    {
                        DrawWarning(config);
                    }
                }
            }

            var footer = count > 0
                ? $"Showing {shownCount} out of {count} registered prefabs."
                : $"No {nameof(CoherenceSync)} prefabs registered.";

            EditorGUILayout.LabelField(footer, ContentUtils.GUIStyles.centeredGreyMiniLabelWrap);
        }

        private void DrawWarning(CoherenceSyncConfig config)
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            var iconRect = lastRect;
            iconRect.xMax = iconRect.xMin + 20f;
            iconRect.x -= 19f;
            var content = GetWarning(config);
            GUI.Label(iconRect, content);
        }

        private GUIContent GetWarning(CoherenceSyncConfig config)
        {
            if (!cachedContent.TryGetValue(config, out var content))
            {
                content = EditorGUIUtility.TrTextContentWithIcon(string.Empty, GetStatusTooltip(config), "Warning");
                cachedContent[config] = content;
            }

            return content;
        }

        private string GetStatusTooltip(CoherenceSyncConfig config)
        {
            return analyzer.GetCompoundedErrorMessage(config);
        }
    }
}
