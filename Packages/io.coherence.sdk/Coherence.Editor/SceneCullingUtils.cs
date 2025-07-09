// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEditor.SceneManagement;

    public static class SceneCullingUtils
    {
        public const ulong defaultMask = 0xE000000000000000UL;

        public static void Set(Scene scene, ulong m)
        {
            var mask = EditorSceneManager.GetSceneCullingMask(scene);
            EditorSceneManager.SetSceneCullingMask(scene, mask | m);
        }

        public static void Unset(Scene scene, ulong m)
        {
            var mask = EditorSceneManager.GetSceneCullingMask(scene);
            EditorSceneManager.SetSceneCullingMask(scene, mask & ~m);
        }

        public static void SetVisible(Scene scene, bool visible)
        {
            if (visible)
            {
                Set(scene, defaultMask);
            }
            else
            {
                Unset(scene, defaultMask);
            }
        }

        public static bool IsVisible(Scene scene)
        {
            return (defaultMask & EditorSceneManager.GetSceneCullingMask(scene)) != 0;
        }
    }
}
