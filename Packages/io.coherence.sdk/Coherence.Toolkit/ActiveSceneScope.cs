// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
#if UNITY_EDITOR
    using System.Reflection;
    using UnityEditor.SceneManagement;
#endif

    public struct ActiveSceneScope : System.IDisposable
    {
#if UNITY_EDITOR
        static ActiveSceneScope()
        {
            try
            {
                var setMethodInfo = typeof(EditorSceneManager).GetMethod("SetTargetSceneForNewGameObjects", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new System.Type[] { typeof(int) }, null);
                SetTargetSceneForNewGameObjects = setMethodInfo.CreateDelegate(typeof(SetTargetSceneCallback)) as SetTargetSceneCallback;
                var clearMethodInfo = typeof(EditorSceneManager).GetMethod("ClearTargetSceneForNewGameObjects", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                ClearTargetSceneForNewGameObjects = clearMethodInfo.CreateDelegate(typeof(ClearTargetSceneCallback)) as ClearTargetSceneCallback;
            }
            catch
            {
                couldReflect = false;
            }
        }

        private delegate void SetTargetSceneCallback(int sceneHandle);
        private delegate void ClearTargetSceneCallback();

        private static readonly SetTargetSceneCallback SetTargetSceneForNewGameObjects;
        private static readonly ClearTargetSceneCallback ClearTargetSceneForNewGameObjects;

        private static readonly bool couldReflect = true;
#endif

        /// <summary>
        /// The scene that was active before this scope.
        /// </summary>
        public readonly Scene currentScene;

        /// <summary>
        /// The scene where new GameObjects will be placed, throughout the scope.
        /// </summary>
        public readonly Scene activeScene;

        public ActiveSceneScope(Component component) : this(component.gameObject) { }
        public ActiveSceneScope(GameObject gameObject) : this(gameObject.scene) { }
        public ActiveSceneScope(Scene scene)
        {
            currentScene = SceneManager.GetActiveScene();
            activeScene = scene;

#if UNITY_EDITOR
            if (couldReflect)
            {
                SetTargetSceneForNewGameObjects(scene.handle);
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeState;
            }
            else
            {
                _ = SceneManager.SetActiveScene(scene);
            }
#else
            _ = SceneManager.SetActiveScene(scene);
#endif
        }

#if UNITY_EDITOR

        // Ensure we don't get Editor crash if user forgot to dispose
        private void OnPlayModeState(UnityEditor.PlayModeStateChange mode)
        {
            if (mode == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                ClearTargetSceneForNewGameObjects();
            }
        }
#endif

        public void Dispose()
        {
#if UNITY_EDITOR
            if (couldReflect)
            {
                ClearTargetSceneForNewGameObjects();
            }
            else
            {
                _ = SceneManager.SetActiveScene(currentScene);
            }
#else
            _ = SceneManager.SetActiveScene(currentScene);
#endif
        }
    }
}
