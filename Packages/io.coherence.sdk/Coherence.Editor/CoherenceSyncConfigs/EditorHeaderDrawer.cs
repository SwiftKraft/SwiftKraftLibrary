// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class EditorHeaderDrawer
    {
        private static readonly Type prefabImporterEditorType =
            Type.GetType("UnityEditor.PrefabImporterEditor, UnityEditor.CoreModule");

        private static readonly Type gameObjectInspectorType =
            Type.GetType("UnityEditor.GameObjectInspector, UnityEditor.CoreModule");

        private static class GUIContents
        {
            public static readonly GUIContent syncWithOn = Icons.GetContentWithText("Logo.Icon", "<color=#29abe2>Sync with </color>"); // text color matches Logo.Icon color
            public static readonly GUIContent syncWithOff = Icons.GetContentWithText("Logo.Icon.Disabled", "<color=#868686>Sync with </color>"); // text color matches Logo.Icon.Disabled color
            public static readonly GUIContent coherence = Icons.GetContent("Logo.Text.Optimized");
            public static readonly GUIContent openInWindow = EditorGUIUtility.TrIconContent("ToolSettings", "Open CoherenceSync Objects");
            public static readonly GUIContent selectRoots = EditorGUIUtility.TrIconContent("Update-Available", "Select the Root(s)");
        }

        static EditorHeaderDrawer()
        {
            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        private static void OnPostHeaderGUI(Editor editor)
        {
            if (gameObjectInspectorType.IsInstanceOfType(editor))
            {
                DrawGameObjectGUI(editor);
            }
            else if (prefabImporterEditorType.IsInstanceOfType(editor))
            {
                DrawPrefabAssetGUI(editor);
            }
        }

        private static void SelectRoots()
        {
            Selection.objects = Selection.objects.Select(obj =>
            {
                switch (obj)
                {
                    case GameObject gameObject:
                        {
                            var sync = gameObject.GetComponentInParent<CoherenceSync>(true);
                            return sync ? sync.gameObject : obj;
                        }
                    case CoherenceSync _:
                        return obj;
                    case Component component:
                        {
                            var sync = component.gameObject.GetComponentInParent<CoherenceSync>(true);
                            return sync ? sync.gameObject : obj;
                        }
                    default:
                        return obj;
                }
            }).ToArray();
        }

        private static bool DrawControls(bool value, bool showSelectRoots)
        {
            EditorGUI.BeginDisabledGroup(CloneMode.Enabled);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
            var bgRect = rect;
            bgRect.xMin -= 4f;
            bgRect.xMax += 4f;
            EditorGUI.DrawRect(bgRect, Color.black);
            var labelStyle = ContentUtils.GUIStyles.richWhiteLabel;
            var content = value ? GUIContents.syncWithOn : GUIContents.syncWithOff;

            var logoRect = rect;
            logoRect.y += 3f;
            logoRect.xMin = labelStyle.CalcSize(content).x + 20f;
            logoRect.width = GUIStyle.none.CalcSize(GUIContents.coherence).x;
            GUI.Label(logoRect, GUIContents.coherence, GUIStyle.none);

            EditorGUI.BeginDisabledGroup(showSelectRoots);
            var toggleRect = rect;
            toggleRect.xMax = logoRect.xMax;
            value = EditorGUI.ToggleLeft(toggleRect, content, value, labelStyle);
            EditorGUI.EndDisabledGroup();

            // avoid button clicks to trigger a change for this control
            var changed = GUI.changed;
            if (showSelectRoots)
            {
                var selectRootsRect = bgRect;
                selectRootsRect.yMin += 2f;
                selectRootsRect.width = 18f;
                selectRootsRect.x = logoRect.xMax + 2f;

                if (GUI.Button(selectRootsRect, GUIContents.selectRoots, ContentUtils.GUIStyles.iconButton))
                {
                    SelectRoots();
                }

                GUI.changed = changed;
            }

            var registryButtonRect = bgRect;
            registryButtonRect.yMin += 2f;
            registryButtonRect.xMin = registryButtonRect.xMax - 18f;

            EditorGUI.EndDisabledGroup();

            changed = GUI.changed;
            if (GUI.Button(registryButtonRect, GUIContents.openInWindow, ContentUtils.GUIStyles.iconButton))
            {
                CoherenceSyncObjectsStandaloneWindow.Open();
            }
            GUI.changed = changed;

            return value;
        }

        private static void DrawPrefabAssetGUI(Editor editor)
        {
            var hasRegistered = false;
            var hasUnregistered = false;
            foreach (var target in editor.targets)
            {
                if (target is not AssetImporter assetImporter)
                {
                    continue;
                }

                var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetImporter.assetPath);
                if (gameObject.TryGetComponent(out CoherenceSync _))
                {
                    hasRegistered = true;
                }
                else
                {
                    hasUnregistered = true;
                }

                // in order to determine mixed value, we need to know if there's at least
                // one value of each state (true/false) - only then we can stop iterating
                if (hasRegistered && hasUnregistered)
                {
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = hasRegistered && hasUnregistered;
            var create = DrawControls(hasRegistered, false);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    AssetDatabase.StartAssetEditing();

                    var prefabPathsWithMissingScripts = new List<string>();

                    foreach (var target in editor.targets)
                    {
                        if (target is not AssetImporter assetImporter)
                        {
                            continue;
                        }

                        var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetImporter.assetPath);
                        var hasSync = gameObject.TryGetComponent(out CoherenceSync _);

                        if (create && !hasSync)
                        {
                            Undo.AddComponent<CoherenceSync>(gameObject);
                        }
                        else if (!create && hasSync)
                        {
                            if (PrefabUtility.IsPartOfAnyPrefab(gameObject) &&
                                !PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(gameObject))
                            {
                                prefabPathsWithMissingScripts.Add(AssetDatabase.GetAssetPath(gameObject));
                                continue;
                            }

                            CoherenceSyncUtils.DestroyCoherenceComponents(gameObject);
                        }
                    }

                    if (prefabPathsWithMissingScripts.Count > 0)
                    {
                        _ = EditorUtility.DisplayDialog("Can't save Prefab",
                            $"Prefabs cannot be saved while they contain missing scripts. The following prefabs were not saved:\n\n{string.Join("\n", prefabPathsWithMissingScripts)}",
                            "OK");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                GUIUtility.ExitGUI();
            }
        }

        private static void DrawGameObjectGUI(Editor editor)
        {
            var hasValidSync = false;
            var hasInvalidSync = false;
            var needsToConvertToPrefab = false;
            var hasPrefabAssetMissing = false;
            var needsToApplyPrefabOverride = false;
            var hasDeepHierarchySelection = false;

            foreach (var target in editor.targets)
            {
                if (EditorUtility.IsPersistent(target))
                {
                    return;
                }

                if (target is not GameObject gameObject)
                {
                    continue;
                }

                var sync = gameObject.GetComponentInParent<CoherenceSync>(true);

                needsToConvertToPrefab |= !PrefabUtility.IsPartOfAnyPrefab(gameObject) &&
                                          !PrefabStageUtility.GetPrefabStage(gameObject);

                hasPrefabAssetMissing |= PrefabUtility.IsPrefabAssetMissing(gameObject);
                needsToApplyPrefabOverride |= sync && PrefabUtility.IsAddedComponentOverride(sync);
                hasDeepHierarchySelection |= sync && sync.gameObject != gameObject;

                if (sync && !needsToConvertToPrefab && !hasPrefabAssetMissing && !needsToApplyPrefabOverride)
                {
                    hasValidSync = true;
                }
                else
                {
                    hasInvalidSync = true;
                }

                // in order to determine mixed value, we need to know if there's at least
                // one value of each state (true/false) - only then we can stop iterating
                if (hasValidSync && hasInvalidSync)
                {
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = hasValidSync && hasInvalidSync;
            var create = DrawControls(hasValidSync, hasDeepHierarchySelection);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                var shouldCreatePrefabAsset = needsToConvertToPrefab || hasPrefabAssetMissing;
                if (create && shouldCreatePrefabAsset && !EditorUtility.DisplayDialog("Sync with coherence",
                        "To allow for networking, GameObjects must be converted to Prefabs. Do you want to continue?",
                        "Convert to Prefab", "Cancel"))
                {
                    return;
                }

                try
                {
                    AssetDatabase.StartAssetEditing();
                    var prefabPathsWithMissingScripts = new List<string>();
                    foreach (var target in editor.targets)
                    {
                        if (target is not GameObject gameObject)
                        {
                            continue;
                        }

                        var stage = PrefabStageUtility.GetPrefabStage(gameObject);
                        var prefabAsset = stage
                            ? stage.prefabContentsRoot
                            : CoherenceSyncEditor.GetPrefab(gameObject) ?? gameObject;

                        var has = gameObject.TryGetComponent(out CoherenceSync _);
                        if (create)
                        {
                            var prefabPath = $"Assets/{prefabAsset.name}.prefab";
                            CoherenceSyncUtils.TryConvertToCoherenceSyncPrefab(gameObject, prefabPath, out _);
                        }
                        else if (has)
                        {
                            if (PrefabUtility.IsPartOfAnyPrefab(prefabAsset) &&
                                !PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(prefabAsset))
                            {
                                prefabPathsWithMissingScripts.Add(AssetDatabase.GetAssetPath(prefabAsset));
                                continue;
                            }

                            CoherenceSyncUtils.DestroyCoherenceComponents(prefabAsset);
                        }
                    }

                    if (prefabPathsWithMissingScripts.Count > 0)
                    {
                        _ = EditorUtility.DisplayDialog("Can't save Prefab",
                            $"Prefabs cannot be saved while they contain missing scripts. The following prefabs were not saved:\n\n{string.Join("\n", prefabPathsWithMissingScripts)}",
                            "OK");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
    }
}
