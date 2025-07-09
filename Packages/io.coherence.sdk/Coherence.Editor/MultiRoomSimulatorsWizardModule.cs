// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using Simulator;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public class MultiRoomSimulatorsWizardModule : HubModule
    {
        private static class ModuleGUIContents
        {
            public static readonly GUIContent about = new("Multi-Room Simulators are Room Simulators which are able to simulate multiple game rooms at the same time. This wizard can help set up scenes to allow for multi-room simulators support.");
            public static readonly GUIContent playModeWarning = new("This wizard is not available in play mode.");
            public static readonly GUIContent ok = EditorGUIUtility.TrIconContent("Installed");
            public static readonly GUIContent warning = EditorGUIUtility.TrIconContent("Warning");
        }

        public override string ModuleName => "Multi-Room Simulators";
        public override HelpSection Help => new()
        {
            title = new GUIContent("What are Multi-Room Simulators"),
            content = ModuleGUIContents.about,
        };

        private Vector2 scroll;

        private SceneAsset menuScene;
        private SceneAsset gameScene;

        private SceneAsset oneScene;

        private ComponentCache<CoherenceBridge> bridges = new();
        private ComponentCache<CoherenceLiveQuery> liveQueries = new();

        private ComponentCache<CoherenceSceneLoader> loaders = new();
        private ComponentCache<CoherenceScene> coherenceScenes = new();
#pragma warning disable CS0618 // Type or member is obsolete
        private ComponentCache<MultiRoomSimulatorLocalForwarder> localForwarders = new();
        private ComponentCache<MultiRoomSimulator> multiRoomSims = new();
#pragma warning restore CS0618 // Type or member is obsolete

        [Serializable]
        private sealed class ComponentCache<T> where T : Component
        {
            public T[] objects;
            public List<T> inScene = new();
            public bool expanded;
            public T selected;

            public void Fetch()
            {
#if UNITY_2023_1_OR_NEWER
                objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                objects = FindObjectsOfType<T>(true);
#endif
                inScene.Clear();
                // NOTE reset selected on edit <-> play mode
            }

            public void GetAllFromScene(Scene s)
            {
                inScene.Clear();

                if (!s.IsValid() || !s.isLoaded)
                {
                    return;
                }

                foreach (var c in objects)
                {
                    if (!c)
                    {
                        continue;
                    }

                    if (c.gameObject.scene != s)
                    {
                        continue;
                    }

                    inScene.Add(c);
                }
            }
        }

        private void FetchComponents()
        {
            bridges.Fetch();
            liveQueries.Fetch();
            loaders.Fetch();
            coherenceScenes.Fetch();
            localForwarders.Fetch();
            multiRoomSims.Fetch();
        }

        public override void OnModuleEnable()
        {
            base.OnModuleEnable();

            Undo.undoRedoPerformed += OnUndoRedo;
            EditorSceneManager.sceneDirtied += OnSceneDirtied;
            EditorBuildSettings.sceneListChanged += OnSceneListChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            FetchComponents();
        }

        public override void OnModuleDisable()
        {
            base.OnModuleDisable();

            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorSceneManager.sceneDirtied -= OnSceneDirtied;
            EditorBuildSettings.sceneListChanged -= OnSceneListChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // objects are recreated between edit and play mode.
            // Up until we properly convert back and forth (see InspectComponentWindow's OnPlayModeStateChanged for an example),
            // it's best to just close the window to avoid confusing users.

            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    MultiRoomSimulatorsWizardModuleWindow.OpenWindow(false);
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    MultiRoomSimulatorsWizardModuleWindow.OpenWindow(false);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private void OnSceneListChanged()
        {
            FetchComponents();
            EditorWindow.GetWindow<MultiRoomSimulatorsWizardModuleWindow>().Repaint();
        }

        private void OnSceneDirtied(Scene scene)
        {
            FetchComponents();
            EditorWindow.GetWindow<MultiRoomSimulatorsWizardModuleWindow>().Repaint();
        }

        private void OnUndoRedo()
        {
            FetchComponents();
            EditorWindow.GetWindow<MultiRoomSimulatorsWizardModuleWindow>().Repaint();
        }

        private bool TryGetFirstScene(SceneAsset asset, out Scene scene)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid())
                {
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (s.path != assetPath)
                {
                    continue;
                }

                scene = s;
                return true;
            }

            scene = default;
            return false;
        }

        private bool DrawSceneField(ref SceneAsset asset, out Scene scene, Action<Scene> onOpen = null)
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var a = EditorGUILayout.ObjectField(asset, typeof(SceneAsset), false) as SceneAsset;
            if (EditorGUI.EndChangeCheck())
            {
                asset = a;
                if (TryGetFirstScene(asset, out scene) && scene.isLoaded)
                {
                    if (onOpen != null)
                    {
                        var s = scene;
                        EditorApplication.delayCall += () => onOpen.Invoke(s);
                    }
                }
            }

            var isOpen = TryGetFirstScene(asset, out scene);
            var isLoaded = isOpen && scene.isLoaded;

            EditorGUI.BeginDisabledGroup(!asset);
            EditorGUI.BeginDisabledGroup(isOpen && isLoaded);
            if (GUILayout.Button("Open", EditorStyles.miniButtonLeft))
            {
                var path = AssetDatabase.GetAssetPath(asset);
                var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                FetchComponents();
                if (onOpen != null)
                {
                    EditorApplication.delayCall += () => onOpen.Invoke(s);
                }
            }

            EditorGUI.EndDisabledGroup();
#if UNITY_2022_2_OR_NEWER
            EditorGUI.BeginDisabledGroup(!isOpen || !isLoaded || SceneManager.loadedSceneCount == 1);
#else
            EditorGUI.BeginDisabledGroup(!isOpen || !isLoaded || EditorSceneManager.loadedSceneCount == 1);
#endif
            if (GUILayout.Button("Close", EditorStyles.miniButtonRight))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    _ = EditorSceneManager.CloseScene(scene, true);
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            return scene.IsValid() && scene.isLoaded;
        }

        private int GetEditorBuildSettingsSceneIndex(string assetPath)
        {
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                var s = scenes[i];
                if (s.path != assetPath)
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        private bool Button(string text, bool warn = true)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.miniButton);
            rect = EditorGUI.IndentedRect(rect);
            if (warn)
            {
                var warnRect = rect;
                warnRect.x -= 1;
                warnRect.width = 20;
                GUI.Label(warnRect, ModuleGUIContents.warning);
                rect.xMin += 20;
            }

            return GUI.Button(rect, text, EditorStyles.miniButton);
        }

        private bool DrawSceneInBuildSettings(Scene scene)
        {
            if (!scene.IsValid())
            {
                EditorGUILayout.LabelField("Invalid scene.");
                return false;
            }

            var ok = true;

            _ = EditorGUILayout.BeginHorizontal();

            var idx = GetEditorBuildSettingsSceneIndex(scene.path);
            if (idx != -1)
            {
                var s = EditorBuildSettings.scenes[idx];
                if (s.enabled)
                {
                    OkLabelGlobal("Scene registered and enabled in Build Settings.");
                }
                else
                {
                    if (Button("Enable scene in Build Settings"))
                    {
                        s.enabled = true;
                        var scenes = EditorBuildSettings.scenes;
                        scenes[idx] = s;
                        EditorBuildSettings.scenes = scenes;
                    }

                    ok = false;
                }
            }
            else
            {
                if (Button("Add scene to Build Settings"))
                {
                    var scenes = EditorBuildSettings.scenes;
                    ArrayUtility.Add(ref scenes, new EditorBuildSettingsScene(scene.path, true));
                    EditorBuildSettings.scenes = scenes;
                }

                ok = false;
            }

            EditorGUILayout.EndHorizontal();

            return ok;
        }

        private static void OkLabelGlobal(string text)
        {
            const int iconWidth = 22;

            var labelStyle = EditorStyles.miniLabel;
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, labelStyle);
            var labelRect = rect;
            labelRect.xMax -= iconWidth;
            EditorGUI.LabelField(labelRect, text, labelStyle);

            var iconRect = rect;
            iconRect.x += iconRect.width - iconWidth;
            iconRect.width = iconWidth;
            GUI.Label(iconRect, ModuleGUIContents.ok);
        }

        private static void OkLabel(string text)
        {
            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContentWithIcon(text, "Installed"));
        }

        private static void WarnLabel(string text)
        {
            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContentWithIcon(text, "Warning"));
        }

        private void InfoLabel(string text)
        {
            GUILayout.Label(text, ContentUtils.GUIStyles.miniLabelGreyWrap);
        }

        private bool DrawRequirementPresentInScene<T>(Scene scene, ComponentCache<T> cache, string newName) where T : Component
        {
            return DrawRequirementPresentInScene(
            () => cache.inScene.Count == 0,
            () => ObjectFactory.CreateGameObject(scene, HideFlags.None, newName, typeof(T)));
        }

        private bool DrawRequirementPresentInScene(Func<bool> shouldCreateFn, Func<Object> createFn)
        {
            if (shouldCreateFn.Invoke())
            {
                if (Button("Create in scene"))
                {
                    var obj = createFn?.Invoke();
                    if (obj)
                    {
                        Undo.RegisterCreatedObjectUndo(obj, "Create in scene");
                    }

                    // NOTE not every GO creation API triggers sceneDirtied
                    FetchComponents();
                    return false;
                }

                return false;
            }

            OkLabel("Present in scene.");
            return true;
        }

        private bool DrawRequirementOnlyOne<T>(Scene scene, ComponentCache<T> cache) where T : Component
        {
            if (cache.inScene.Count > 1)
            {
                var content = EditorGUIUtility.TrTextContentWithIcon("A scene should have only one.", "Warning");
                EditorGUILayout.LabelField(content);
                EditorGUI.indentLevel++;
                foreach (var c in cache.inScene)
                {
                    if (Button("Destroy " + c.name))
                    {
                        Undo.DestroyObjectImmediate(c.gameObject);
                    }
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField("...", ContentUtils.GUIStyles.miniLabelGrey);
                return false;
            }

            OkLabel("Only one.");
            return true;
        }

        private bool DrawRequirementEnabled<T>(T b) where T : Behaviour
        {
            if (!b.enabled)
            {
                if (Button("Enable"))
                {
                    Undo.RecordObject(b, "Enable");
                    b.enabled = true;
                    EditorUtility.SetDirty(b);
                }

                return false;
            }

            OkLabel("Enabled.");
            return true;
        }

        private bool DrawRequirementActiveInHierarchy<T>(T c) where T : Component
        {
            if (!c.gameObject.activeInHierarchy)
            {
                if (Button("Activate in hierarchy"))
                {
                    var go = c.gameObject;
                    while (!go.activeInHierarchy)
                    {
                        if (!go.activeSelf)
                        {
                            Undo.RecordObject(go, "Activate in hierarchy");
                            go.SetActive(true);
                            EditorUtility.SetDirty(go);
                        }

                        if (!go.transform.parent)
                        {
                            break;
                        }

                        go = go.transform.parent.gameObject;
                    }
                }

                return false;
            }

            OkLabel("Active in hierarchy.");
            return true;
        }

        private bool DrawRequirement<T>(T c, Func<T, bool> validator, Action<T> fn, string actionText, string okText) where T : Object
        {
            if (!validator(c))
            {
                if (Button(actionText))
                {
                    Undo.RecordObject(c, actionText);
                    fn(c);
                }

                return false;
            }

            OkLabel(okText);
            return true;
        }

        private bool DrawProperty(Object o, string propertyName)
        {
            using var so = new SerializedObject(o);
            using var p = so.FindProperty(propertyName);
            var expanded = EditorGUILayout.PropertyField(p);
            _ = so.ApplyModifiedProperties();
            return expanded;
        }

        private class DrawRequirementScope<T> : IDisposable where T : Component
        {
            public ComponentCache<T> cache;
            public bool ok = true;

            private Rect headerRect;
            private const int iconWidth = 22;

            public DrawRequirementScope(ComponentCache<T> cache, Scene scene)
            {
                this.cache = cache;
                cache.GetAllFromScene(scene);
                var style = EditorStyles.foldoutHeader;
                headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, style);
                var foldoutRect = headerRect;
                foldoutRect.xMax -= iconWidth;
                cache.expanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, cache.expanded, typeof(T).Name, style);
                EditorGUI.indentLevel++;
            }

            public void Dispose()
            {
                var iconRect = headerRect;
                iconRect.x += iconRect.width - iconWidth;
                iconRect.width = iconWidth;
                GUI.Label(iconRect, ok ? ModuleGUIContents.ok : ModuleGUIContents.warning);

                EditorGUI.indentLevel--;
                EditorGUI.EndFoldoutHeaderGroup();
            }
        }

        private bool DrawBridgeRequirements(Scene scene)
        {
            using var scope = new DrawRequirementScope<CoherenceBridge>(bridges, scene);
            if (scope.cache.expanded)
            {
                if (!DrawRequirementPresentInScene(scene, scope.cache, "coherence Bridge"))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                if (!DrawRequirementOnlyOne(scene, scope.cache))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                var bridge = scope.cache.inScene[0];
                scope.ok &= DrawRequirementEnabled(bridge);
                scope.ok &= DrawRequirementActiveInHierarchy(bridge);
                scope.ok &= DrawRequirement(bridge, b => !b.mainBridge, b =>
                {
                    b.mainBridge = false;
                    EditorUtility.SetDirty(b);
                }, "Mark as non-master", "Not a master Bridge.");
                scope.ok &= DrawRequirement(bridge, b => b.EnableClientConnections, b =>
                {
                    b.EnableClientConnections = true;
                    EditorUtility.SetDirty(b);
                }, "Use Global Query", "Uses Global Query.");
                scope.ok &= DrawRequirement(bridge, b => !b.controlTimeScale, b =>
                {
                    b.controlTimeScale = false;
                    EditorUtility.SetDirty(b);
                }, "Disable Control Time Scale", "Disabled Control Time Scale.");

                return scope.ok;
            }

            if (scope.cache.inScene.Count != 1)
            {
                scope.ok = false;
                return scope.ok;
            }

            var o = scope.cache.inScene[0];
            scope.ok = o.enabled && o.gameObject.activeInHierarchy && !o.mainBridge && o.EnableClientConnections;
            return scope.ok;
        }

        private bool DrawLiveQueryRequirements(Scene scene)
        {
            using var scope = new DrawRequirementScope<CoherenceLiveQuery>(liveQueries, scene);
            if (scope.cache.expanded)
            {
                if (!DrawRequirementPresentInScene(scene, scope.cache, "coherence Live Query"))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                if (!DrawRequirementOnlyOne(scene, scope.cache))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                var liveQuery = scope.cache.inScene[0];
                scope.ok &= DrawRequirementEnabled(liveQuery);
                scope.ok &= DrawRequirementActiveInHierarchy(liveQuery);

                return scope.ok;
            }

            if (scope.cache.inScene.Count != 1)
            {
                scope.ok = false;
                return scope.ok;
            }

            var o = scope.cache.inScene[0];
            scope.ok = o.enabled && o.gameObject.activeInHierarchy;

            return scope.ok;
        }

        private bool DrawMultiRoomSimulatorLocalForwarderRequirements(Scene scene)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            using var scope = new DrawRequirementScope<MultiRoomSimulatorLocalForwarder>(localForwarders, scene);
#pragma warning restore CS0618 // Type or member is obsolete
            if (scope.cache.expanded)
            {
                if (!DrawRequirementPresentInScene(scene, scope.cache, "coherence Multi-Room Simulator Local Forwarder"))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                if (!DrawRequirementOnlyOne(scene, scope.cache))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                var localForwarder = scope.cache.inScene[0];
                scope.ok &= DrawRequirementEnabled(localForwarder);
                scope.ok &= DrawRequirementActiveInHierarchy(localForwarder);

                return scope.ok;
            }

            if (scope.cache.inScene.Count != 1)
            {
                scope.ok = false;
                return scope.ok;
            }

            var o = scope.cache.inScene[0];
            scope.ok = o.enabled && o.gameObject.activeInHierarchy;
            return scope.ok;
        }

        private bool DrawMultiRoomSimulatorRequirements(Scene scene)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            using var scope = new DrawRequirementScope<MultiRoomSimulator>(multiRoomSims, scene);
#pragma warning restore CS0618 // Type or member is obsolete
            if (scope.cache.expanded)
            {
                if (!DrawRequirementPresentInScene(scene, scope.cache, "coherence Multi-Room Simulator"))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                if (!DrawRequirementOnlyOne(scene, scope.cache))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                var mrs = scope.cache.inScene[0];
                scope.ok &= DrawRequirementEnabled(mrs);
                scope.ok &= DrawRequirementActiveInHierarchy(mrs);

                return scope.ok;
            }

            if (scope.cache.inScene.Count != 1)
            {
                scope.ok = false;
                return scope.ok;
            }

            var o = scope.cache.inScene[0];
            scope.ok = o.enabled && o.gameObject.activeInHierarchy;

            return scope.ok;
        }

        private bool DrawCoherenceSceneRequirements(Scene scene, bool smartSelect = false)
        {
            using var scope = new DrawRequirementScope<CoherenceScene>(coherenceScenes, scene);
            if (scope.cache.expanded)
            {
                if (!DrawRequirementPresentInScene(scene, scope.cache, "coherence Scene"))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                if (!DrawRequirementOnlyOne(scene, scope.cache))
                {
                    scope.ok = false;
                    return scope.ok;
                }

                const float minLabelWidth = 225f;
                if (EditorGUIUtility.labelWidth < minLabelWidth)
                {
                    EditorGUIUtility.labelWidth =  minLabelWidth;
                }

                var coherenceScene = scope.cache.inScene[0];
                scope.ok &= DrawRequirementEnabled(coherenceScene);
                scope.ok &= DrawRequirementActiveInHierarchy(coherenceScene);
                scope.ok &= DrawRequirement(coherenceScene, s => s.connect, s =>
                {
                    s.connect = true;
                    EditorUtility.SetDirty(s);
                }, "Enable connect", "Handles connection.");

                if (smartSelect)
                {
                    using var so = new SerializedObject(coherenceScene);
                    using var p = so.FindProperty("deactivateOnLoad");
                    if (Button("Deactivate GameObjects on Load", false))
                    {
                        CoherenceSceneEditor.SmartSelect(coherenceScene);
                    }

                    EditorGUI.indentLevel++;
                    for (var i = 0; i < p.arraySize; i++)
                    {
                        _ = EditorGUILayout.PropertyField(p.GetArrayElementAtIndex(i), GUIContent.none);
                    }

                    EditorGUI.indentLevel--;
                    _ = so.ApplyModifiedProperties();
                }

                _ = DrawProperty(coherenceScene, "sceneVisibilityForClient");
                _ = DrawProperty(coherenceScene, "sceneVisibilityForSimulator");
                _ = DrawProperty(coherenceScene, "hideEditorSceneOnDisconnect");

                return scope.ok;
            }

            if (scope.cache.inScene.Count != 1)
            {
                scope.ok = false;
                return scope.ok;
            }

            var o = scope.cache.inScene[0];
            scope.ok = o.enabled && o.gameObject.activeInHierarchy && o.connect;

            return scope.ok;
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (Application.isPlaying)
            {
                CoherenceHubLayout.DrawWarnArea(ModuleGUIContents.playModeWarning);
                return;
            }

            EditorGUILayout.Space();

            DrawSceneSuggestions();
        }

        private void DrawSceneSuggestions()
        {
            if (DrawSceneField(ref oneScene, out var scene, OnOpenSingleScene))
            {
                EditorGUI.indentLevel++;
                _ = DrawSceneInBuildSettings(scene);
                _ = DrawBridgeRequirements(scene);
                _ = DrawLiveQueryRequirements(scene);
                _ = DrawMultiRoomSimulatorLocalForwarderRequirements(scene);
                _ = DrawCoherenceSceneRequirements(scene, true);
                _ = DrawMultiRoomSimulatorRequirements(scene);
                EditorGUI.indentLevel--;
            }
        }

        private void OnOpenSingleScene(Scene scene)
        {
            loaders.GetAllFromScene(scene);
            if (loaders.inScene.Count > 0)
            {
                loaders.selected = loaders.inScene[0];
                EditorUtility.SetDirty(this);
            }

            EditorWindow.GetWindow<MultiRoomSimulatorsWizardModuleWindow>().Repaint();
        }
    }
}
