// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using Mode = UnityEditor.SceneManagement.PrefabStage.Mode;

    /// <summary>
    /// Utility methods related to prefabs.
    /// </summary>
    internal static class PrefabUtils
    {
        /// <summary>
        /// Opens the <see paramref="gameObject"/> in the Prefab Stage in isolation.
        /// </summary>
        /// <param name="gameObject"> Game object that is part of a prefab instance or an open prefab stage. </param>
        /// <param name="gameObjectStatus"> Status of the <see paramref="gameObject"/>. </param>
        /// <param name="select"> Also select the prefab asset? </param>
        public static void OpenInIsolation(GameObject gameObject, GameObjectStatus gameObjectStatus, bool select)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            string assetPath;
            if(!gameObject.scene.IsValid())
            {
                assetPath = AssetDatabase.GetAssetPath(gameObject);
            }
            else
            {
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

                if(string.IsNullOrEmpty(assetPath) && gameObjectStatus.IsInPrefabStage)
                {
                    assetPath = prefabStage.assetPath;
                }
            }

            if(string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));

            if(!prefabStage || prefabStage.assetPath != assetPath || prefabStage.mode is not Mode.InIsolation)
            {
                PrefabStageUtility.OpenPrefab(assetPath, null, Mode.InIsolation);
            }

            if(select)
            {
                prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                Selection.activeGameObject = prefabStage.prefabContentsRoot;
            }

            if(Event.current != null)
            {
                GUIUtility.ExitGUI();
            }
        }

        /// <summary>
        /// Determines if the component is in an instance of a prefab.
        /// </summary>
        /// <param name="component"><see cref="Component"/> to test.</param>
        /// <returns><see langword="true"/> if the component is part of a prefab instance, <see langword="false" /> otherwise.</returns>
        internal static bool IsInstance(Component component)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var inStage = stage != null && component && stage.prefabContentsRoot == component.gameObject;
            return PrefabUtility.IsPartOfPrefabInstance(component) && !inStage;
        }
    }
}
