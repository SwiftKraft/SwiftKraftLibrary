// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public static class SceneUtils
    {
#if UNITY_2023_1_OR_NEWER
        public static T[] FindInScene<T>(Scene scene, FindObjectsInactive findObjectsInactive,
            FindObjectsSortMode sortMode) where T : Component
        {
            return Object.FindObjectsByType<T>(findObjectsInactive, sortMode)
                .Where(c => c && c.gameObject.scene == scene)
                .ToArray();
        }

        [Obsolete("Use FindInScene overload with FindObjectsInactive and FindObjectsSortMode to avoid sorting when you don't need to.")]
#endif
        public static T[] FindInScene<T>(Scene scene, bool includeInactive = false) where T : Component
        {
            return Object.FindObjectsOfType<T>(includeInactive).Where(c => c && c.gameObject.scene == scene).ToArray();
        }

#if UNITY_2023_1_OR_NEWER
        public static GameObject[] FindInScene(Scene scene, FindObjectsInactive findObjectsInactive,
            FindObjectsSortMode sortMode)
        {
            return Object.FindObjectsByType<GameObject>(findObjectsInactive, sortMode)
                .Where(go => go && go.scene == scene)
                .ToArray();
        }

        [Obsolete("Use FindInScene overload with FindObjectsInactive and FindObjectsSortMode to avoid sorting when you don't need to.")]
#endif
        public static GameObject[] FindInScene(Scene scene, bool includeInactive = false)
        {
            return Object.FindObjectsOfType<GameObject>(includeInactive).Where(go => go && go.scene == scene).ToArray();
        }

#if UNITY_2023_1_OR_NEWER
        public static void FindInScene<T>(Scene scene, List<T> result, FindObjectsInactive findObjectsInactive,
            FindObjectsSortMode sortMode) where T : Component
        {
            result.Clear();
            var components = Object.FindObjectsByType<T>(findObjectsInactive, sortMode);
            foreach (var c in components)
            {
                if (c && c.gameObject.scene == scene)
                {
                    result.Add(c);
                }
            }
        }

        [Obsolete("Use FindInScene overload with FindObjectsInactive and FindObjectsSortMode to avoid sorting when you don't need to.")]
#endif
        public static void FindInScene<T>(Scene scene, List<T> result, bool includeInactive = false) where T : Component
        {
            result.Clear();
            var components = Object.FindObjectsOfType<T>(includeInactive);
            foreach (var c in components)
            {
                if (c && c.gameObject.scene == scene)
                {
                    result.Add(c);
                }
            }
        }

#if UNITY_2023_1_OR_NEWER
        public static void FindInScene(Scene scene, List<GameObject> result, FindObjectsInactive findObjectsInactive,
            FindObjectsSortMode sortMode)
        {
            result.Clear();
            var gameObjects = Object.FindObjectsByType<GameObject>(findObjectsInactive, sortMode);
            foreach (var go in gameObjects)
            {
                if (go && go.scene == scene)
                {
                    result.Add(go);
                }
            }
        }

        [Obsolete("Use FindInScene overload with FindObjectsInactive and FindObjectsSortMode to avoid sorting when you don't need to.")]
#endif

        public static void FindInScene(Scene scene, List<GameObject> result, bool includeInactive = false)
        {
            result.Clear();
            var gameObjects = Object.FindObjectsOfType<GameObject>(includeInactive);
            foreach (var go in gameObjects)
            {
                if (go && go.scene == scene)
                {
                    result.Add(go);
                }
            }
        }
    }
}
