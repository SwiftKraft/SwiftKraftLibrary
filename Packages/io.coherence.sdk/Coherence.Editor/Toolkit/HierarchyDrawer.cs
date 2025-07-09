// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using Coherence.Toolkit;
    using Coherence.Connection;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEditor;

    [InitializeOnLoad]
    internal static class HierarchyDrawer
    {
        private static class GUIContents
        {
            public static readonly GUIContent hasBindings = Icons.GetContent("Coherence.Hierarchy.Simulated");
            public static readonly GUIContent hasBindingsInChildren = Icons.GetContent("Coherence.Hierarchy.Networked");
            public static readonly GUIContent simulated = Icons.GetContent("Coherence.Hierarchy.Simulated", "Having state authority over this object.");
            public static readonly GUIContent networked = Icons.GetContent("Coherence.Hierarchy.Networked", "Not having state authority over this object.");
            public static readonly GUIContent orphan = Icons.GetContent("Coherence.Hierarchy.Orphan");
            public static readonly GUIContent connected = Icons.GetContent("Coherence.Connected", "Connected.");
            public static readonly GUIContent disconnected = Icons.GetContent("Coherence.Disconnected", "Disconnected.");
            public static readonly GUIContent simulator = Icons.GetContent("Coherence.ConnectionType.Simulator", "This scene is connected as a Simulator or host.");
            public static readonly GUIContent input = Icons.GetContent("Coherence.Input", "Having input authority over this object.");
            public static readonly GUIContent inputRemote = Icons.GetContent("Coherence.Input.Remote", "Not having input authority over this object.");
            public static readonly GUIContent client = Icons.GetContent("Coherence.ConnectionType.Client", "Loaded through CoherenceSceneLoader with Client connection type.");

            public static readonly GUIContent loaded = Icons.GetContent("Coherence.Scene.Loaded", "CoherenceScene connection stablished.");

            public static readonly GUIContent hidden = EditorGUIUtility.TrIconContent("scenevis_hidden_hover");
            public static readonly GUIContent visible = EditorGUIUtility.TrIconContent("scenevis_visible_hover");
        }

        private static readonly Dictionary<int, Component> hash = new();
        private static readonly System.Type sceneHierarchyWindowType;
        private static Object[] hierarchyWindows;

        static HierarchyDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            sceneHierarchyWindowType = System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor.dll");
        }

        /// <summary>
        /// Get the expanded state of an Object.
        /// </summary>
        /// <remarks>
        /// All opened scene hierarchy windows are traversed. If the Object is expanded at least on one of them, this method returns true.
        /// This method call might be slow, as it has to find all scene hierarchy windows opened everytime it's invoked, and finds the expanded IDs
        /// using SerializedObject/SerializedProperty APIs.
        /// </remarks>
        private static bool IsExpanded(int id)
        {
            hierarchyWindows = Resources.FindObjectsOfTypeAll(sceneHierarchyWindowType);
            foreach (var hw in hierarchyWindows)
            {
                using (var p = new SerializedObject(hw).FindProperty("m_SceneHierarchy.m_TreeViewState.m_ExpandedIDs"))
                {
                    for (int i = 0; i < p.arraySize; i++)
                    {
                        using (var item = p.GetArrayElementAtIndex(i))
                        {
                            if (id == item.intValue)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (CoherenceSyncBindingsWindow.Instance)
            {
                DrawConfigureTarget(instanceID, selectionRect, CoherenceSyncBindingsWindow.Instance);
            }

            if (Application.isPlaying)
            {
                DrawSceneRuntime(instanceID, selectionRect);
            }
        }

        private static void DrawSceneRuntime(int instanceID, Rect selectionRect)
        {
            // CoherenceSceneLoader

            if (TryGet(instanceID, out CoherenceSceneLoader loader))
            {
                if (loader.Scene.IsValid())
                {
                    EditorGUI.BeginDisabledGroup(!loader.isActiveAndEnabled);
                    var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 16, 16);
                    if (GUI.Button(iconRect, GUIContents.loaded, GUIStyle.none))
                    {
                        if (CoherenceScene.map.TryGetValue(loader.Scene, out CoherenceScene l))
                        {
                            EditorGUIUtility.PingObject(l);
                        }
                        else
                        {
                            EditorGUIUtility.PingObject(loader.Scene.handle);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }

            // CoherenceScene

            if (TryGet(instanceID, out CoherenceScene listener))
            {
                if (listener.Active)
                {
                    EditorGUI.BeginDisabledGroup(!listener.isActiveAndEnabled);
                    var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 16, 16);
                    var hasLoader = CoherenceSceneLoader.loaderMap.TryGetValue(listener.gameObject.scene, out loader);
                    if (GUI.Button(iconRect, GUIContents.loaded, GUIStyle.none))
                    {
                        if (hasLoader)
                        {
                            EditorGUIUtility.PingObject(loader);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }

            // Bridge

            if (TryGet(instanceID, out CoherenceBridge bridge))
            {
                EditorGUI.BeginDisabledGroup(!bridge || !bridge.isActiveAndEnabled);
                var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 16, 16);
                var connected = bridge.Client != null && bridge.Client.IsConnected();
                GUI.Label(iconRect, connected ? GUIContents.connected : GUIContents.disconnected, GUIStyle.none);
                EditorGUI.EndDisabledGroup();
            }

            // Scene

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene != null && scene.IsValid())
                {
                    if (instanceID == scene.handle)
                    {
                        var visibleRect = new Rect(selectionRect.xMax - 20, selectionRect.y, 16, 16);
                        var visible = SceneCullingUtils.IsVisible(scene);

                        if (GUI.Button(visibleRect, visible ? GUIContents.visible : GUIContents.hidden, GUIStyle.none))
                        {
                            SceneCullingUtils.SetVisible(scene, !visible);
                        }

                        if (CoherenceBridgeStore.TryGetBridge(scene, out var sceneBridge) && sceneBridge.Client != null)
                        {
                            var connectedRect = new Rect(visibleRect.xMin - 16, visibleRect.y, 16, 16);
                            if (GUI.Button(connectedRect, sceneBridge.IsConnected ? GUIContents.connected : GUIContents.disconnected, GUIStyle.none))
                            {
                                EditorGUIUtility.PingObject(sceneBridge);
                            }

                            if (sceneBridge.IsConnected)
                            {
                                var connTypeRect = new Rect(connectedRect.xMin - 16, selectionRect.y, 16, 16);
                                var endpointRect = new Rect(connTypeRect.xMin - 80, selectionRect.y, 80, 16);
                                GUI.Label(connTypeRect, sceneBridge.IsSimulatorOrHost ? GUIContents.simulator : GUIContents.client, GUIStyle.none);
                                var epd = sceneBridge.Client.LastEndpointData;
                                GUI.Label(endpointRect, epd.worldId != 0 ? epd.worldId.ToString() : epd.uniqueRoomId.ToString(), ContentUtils.GUIStyles.greyMiniLabelRight);
                            }
                        }
                    }
                }
            }

            // CoherenceSync

            if (TryGet(instanceID, out CoherenceSync sync))
            {
                EditorGUI.BeginDisabledGroup(!sync.isActiveAndEnabled);
                var iconRect = new Rect(32, selectionRect.y, 16, 16);

                GUIContent content;
                if (sync.Input)
                {
                    content = sync.HasInputAuthority ? GUIContents.input : GUIContents.inputRemote;
                }
                else
                {
                    content = sync.HasStateAuthority ? GUIContents.simulated : GUIContents.networked;
                }
                GUI.Label(iconRect, content);

                if (sync.EntityState?.IsOrphaned ?? false)
                {
                    GUI.Label(iconRect, GUIContents.orphan);
                }

                EditorGUI.EndDisabledGroup();
            }
        }

        private static bool HasBindingsOrComponentActions(CoherenceSync root, int instanceID)
        {
            foreach (var binding in root.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (IsPartOfGameObject(binding.unityComponent, instanceID))
                {
                    return true;
                }
            }

            if (root.componentActions != null)
            {
                foreach (var componentAction in root.componentActions)
                {
                    if (componentAction == null)
                    {
                        continue;
                    }

                    if (IsPartOfGameObject(componentAction.component, instanceID))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsPartOfGameObject(Component component, int instanceID)
        {
            return component && component.gameObject.GetInstanceID() == instanceID;
        }

        private static bool IsPartOfChildGameObject(Component component, int instanceID)
        {
            if (!component || !component.gameObject)
            {
                return false;
            }

            var t = component.gameObject.transform.parent;
            while (t != null)
            {
                if (t.gameObject.GetInstanceID() == instanceID)
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private static bool HasBindingsOrComponentActionsInChildren(CoherenceSync root, int instanceID)
        {
            foreach (var binding in root.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (IsPartOfChildGameObject(binding.unityComponent, instanceID))
                {
                    return true;
                }
            }

            if (root.componentActions != null)
            {
                foreach (var componentAction in root.componentActions)
                {
                    if (componentAction == null)
                    {
                        continue;
                    }

                    if (IsPartOfChildGameObject(componentAction.component, instanceID))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void DrawConfigureTarget(int instanceID, Rect selectionRect, CoherenceSyncBindingsWindow window)
        {
            if (!window || !window.Sync)
            {
                return;
            }

            if (HasBindingsOrComponentActions(window.Sync, instanceID))
            {
                var root = window.Sync.gameObject;
                EditorGUI.BeginDisabledGroup(!root.activeInHierarchy);
                var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 16, 16);
                //var iconRect = new Rect(32, selectionRect.y, 16, 16);
                GUI.Label(iconRect, GUIContents.hasBindings);
                EditorGUI.EndDisabledGroup();
            }
            else if (HasBindingsOrComponentActionsInChildren(window.Sync, instanceID) && !IsExpanded(instanceID))
            {
                var root = window.Sync.gameObject;
                EditorGUI.BeginDisabledGroup(!root.activeInHierarchy);
                var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 16, 16);
                //var iconRect = new Rect(32, selectionRect.y, 16, 16);
                GUI.Label(iconRect, GUIContents.hasBindingsInChildren);
                EditorGUI.EndDisabledGroup();
            }
        }

        private static bool TryGet<T>(int instanceID, out T component) where T : Component
        {
            if (hash.TryGetValue(instanceID, out Component c))
            {
                component = c as T;
                if (!component)
                {
                    _ = hash.Remove(instanceID);
                    return false;
                }

                return true;
            }
            else
            {
                var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (!go)
                {
                    component = null;
                    return false;
                }

                var hasSync = go.TryGetComponent(out component);
                if (hasSync)
                {
                    hash.Add(instanceID, component);
                }

                return hasSync;
            }

        }
    }
}
