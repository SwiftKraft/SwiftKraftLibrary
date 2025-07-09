// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Linq;
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    internal static class NetworkObjectsInfoDrawer
    {
        private static float padding => 5f;

        private static GUIContent helpText = new("Networked Prefabs are Prefabs with the CoherenceSync component that coherence can sync over the network.");

        internal static bool DrawInfoWithFoldout(bool foldout, int assetsCount, bool isFiltered, EntryInfo entriesInfo,
            Action onDeletedBindings, Action onDeletedMissingAssets)
        {
            foldout = EditorGUILayout.Foldout(foldout, EditorGUIUtility.TrTextContent("What is synchronized over the network?"));

            if (foldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField(helpText, ContentUtils.GUIStyles.richMiniLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        float maxWidth = EditorGUIUtility.labelWidth;
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(maxWidth * 0.5f)))
                        {
                            DrawAssets(assetsCount, isFiltered);
                            DrawVariables(entriesInfo.Variables);
                        }

                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(maxWidth * 0.25f)))
                        {
                            DrawMethods(entriesInfo.Methods);
                            DrawComponentActions(entriesInfo.ComponentActions);
                        }
                    }
                }
            }

            DrawMissingAssets(entriesInfo.MissingAssets, onDeletedMissingAssets);
            DrawInvalidBindings(entriesInfo.InvalidBindings, onDeletedBindings);
            DrawMissingFromPreloadedAssets();
            EditorGUILayout.Separator();

            return foldout;
        }

        internal static void DrawInvalidBindings(int invalid, Action onDeletedBindings,
            CoherenceSync sync = null)
        {
            var nullStr = invalid > 0
                ? $"You have ({invalid}) invalid binding{(invalid != 1 ? "s" : "")}. It may cause instability."
                : string.Empty;

            if (!string.IsNullOrEmpty(nullStr))
            {
                using var scope = new EditorGUILayout.VerticalScope();
                DrawBackground(scope.rect);

                var content = EditorGUIUtility.TrTextContentWithIcon(nullStr, string.Empty, "Warning");

                EditorGUILayout.LabelField(content, ContentUtils.GUIStyles.richMiniLabel);

                if (CoherenceHubLayout.DrawButton("Remove All Invalid Bindings"))
                {
                    if (!EditorUtility.DisplayDialog("Remove All Invalid Bindings?",
                            "This will clear the invalid bindings found on the current prefab.",
                            "OK", "Cancel"))
                    {
                        return;
                    }

                    if (sync)
                    {
                        CoherenceSyncUtils.RemoveInvalidBindings(sync);
                    }
                    else
                    {
                        foreach (var config in CoherenceSyncConfigRegistry.Instance)
                        {
                            CoherenceSyncUtils.RemoveInvalidBindings(config.Sync);
                        }
                    }

                    onDeletedBindings?.Invoke();
                }

                EditorGUILayout.Separator();
            }
        }

        internal static void DrawBindingsWithInputAuthorityPrediction(CoherenceSync sync, int bindingsWithInputAuth, Action onFixedBindings)
        {
            if (bindingsWithInputAuth == 0 || sync.TryGetComponent<CoherenceInput>(out var input))
            {
                return;
            }

            var warnLabel = $"You have ({bindingsWithInputAuth}) bindings with input authority prediction, but you're not using the CoherenceInput component.";

            using var scope = new EditorGUILayout.VerticalScope();
            DrawBackground(scope.rect);

            var content = EditorGUIUtility.TrTextContentWithIcon(warnLabel, string.Empty, "Warning");

            EditorGUILayout.LabelField(content, ContentUtils.GUIStyles.richMiniLabel);

            if (CoherenceHubLayout.DrawButton("Remove Client Prediction"))
            {
                foreach (var binding in sync.Bindings)
                {
                    if (binding.predictionMode == PredictionMode.InputAuthority)
                    {
                        binding.predictionMode = PredictionMode.Never;
                    }
                }

                EditorUtility.SetDirty(sync);

                onFixedBindings?.Invoke();
            }

            EditorGUILayout.Separator();
        }

        internal static void DrawMissingAssets(int missingAssets, Action onClick)
        {
            if (missingAssets == 0)
            {
                return;
            }

            using var scope = new EditorGUILayout.VerticalScope();
            DrawBackground(scope.rect);

            var message = missingAssets > 0 ? $"Found {missingAssets} missing objects." : string.Empty;
            var content = EditorGUIUtility.TrTextContentWithIcon(message, string.Empty, "Warning");

            EditorGUILayout.LabelField(content);
            if (CoherenceHubLayout.DrawButton("View in Registry"))
            {
                Selection.activeObject = CoherenceSyncConfigRegistry.Instance;
                onClick?.Invoke();
            }
            EditorGUILayout.Separator();
        }

        internal static void DrawMissingFromPreloadedAssets()
        {
            var registry = CoherenceSyncConfigRegistry.Instance;
            var assets = PlayerSettings.GetPreloadedAssets();

            if (AssetDatabase.Contains(registry) && !assets.Contains(registry))
            {
                using var scope = new EditorGUILayout.VerticalScope();
                DrawBackground(scope.rect);

                var content = EditorGUIUtility.TrTextContentWithIcon(
                    "CoherenceSyncConfigRegistry should be part of Unity preloaded assets.", string.Empty, "Warning");

                EditorGUILayout.LabelField(content);

                if (CoherenceHubLayout.DrawButton("Add To Preloaded Assets"))
                {
                    for (int i = 0; i < assets.Length; i++)
                    {
                        if (!assets[i])
                        {
                            ArrayUtility.RemoveAt(ref assets, i);
                            i--;
                        }
                    }

                    ArrayUtility.Add(ref assets, registry);
                    PlayerSettings.SetPreloadedAssets(assets);
                }

                EditorGUILayout.Separator();
            }
        }

        private static void DrawAssets(int assetsCount, bool isFiltered)
        {
            var content = EditorGUIUtility.TrTempContent(
                $"{(assetsCount != 0 ? assetsCount.ToString() : "No")} <color=grey>{(isFiltered ? "filtered assets" : "assets")}</color>");

            var width = ContentUtils.GUIStyles.richMiniLabel.CalcSize(content).x;
            var controlRect = EditorGUILayout.GetControlRect(false,
                ContentUtils.GUIStyles.richMiniLabel.CalcHeight(content, width + padding),
                ContentUtils.GUIStyles.richMiniLabel, GUILayout.Width(width + padding));

            EditorGUI.LabelField(controlRect, content, ContentUtils.GUIStyles.richMiniLabel);
        }

        private static void DrawVariables(int variables)
        {
            var content = EditorGUIUtility.TrTempContent(
                $"{(variables != 0 ? variables.ToString() : "No")} <color=grey>variable{(variables != 1 ? "s" : "")}</color>");

            var width = ContentUtils.GUIStyles.richMiniLabel.CalcSize(content).x;

            var controlRect = EditorGUILayout.GetControlRect(false,
                ContentUtils.GUIStyles.richMiniLabel.CalcHeight(content, width + padding),
                ContentUtils.GUIStyles.richMiniLabel, GUILayout.Width(width + padding));

            EditorGUI.LabelField(controlRect, content, ContentUtils.GUIStyles.richMiniLabel);
        }

        private static void DrawBackground(Rect rect)
        {
            string colorHex = EditorGUIUtility.isProSkin ? "#3f3f3f" : "#c8c8c8";

            ColorUtility.TryParseHtmlString(colorHex, out Color color);

            EditorGUI.DrawRect(rect, color);
        }

        private static void DrawMethods(int methods)
        {
            var content = EditorGUIUtility.TrTempContent(
                $"{(methods != 0 ? methods.ToString() : "No")} <color=grey>method{(methods != 1 ? "s" : "")}</color>");

            var width = ContentUtils.GUIStyles.richMiniLabel.CalcSize(content).x;

            var controlRect = EditorGUILayout.GetControlRect(false,
                ContentUtils.GUIStyles.richMiniLabel.CalcHeight(content, width + padding),
                ContentUtils.GUIStyles.richMiniLabel, GUILayout.Width(width + padding));

            EditorGUI.LabelField(controlRect, content, ContentUtils.GUIStyles.richMiniLabel);
        }

        private static void DrawComponentActions(int componentActions)
        {
            GUIContent content = EditorGUIUtility.TrTempContent(
                $"{(componentActions != 0 ? componentActions.ToString() : "No")} <color=grey>component action{(componentActions != 1 ? "s" : "")}</color>");
            var width = ContentUtils.GUIStyles.richMiniLabel.CalcSize(content).x;

            var controlRect = EditorGUILayout.GetControlRect(false,
                ContentUtils.GUIStyles.richMiniLabel.CalcHeight(content, width + padding),
                ContentUtils.GUIStyles.richMiniLabel, GUILayout.Width(width + padding));

            EditorGUI.LabelField(controlRect, content, ContentUtils.GUIStyles.richMiniLabel);
        }
    }
}
