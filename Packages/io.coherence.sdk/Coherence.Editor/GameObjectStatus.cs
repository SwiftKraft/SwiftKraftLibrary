namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    /// <summary>
    /// Object that can be used to acquire information about the status of a game object at a particular moment in time.
    /// <para>
    /// This includes information like whether the game object is part of a prefab, a prefab instance, or open in a prefab stage.
    /// </para>
    /// </summary>
    public readonly struct GameObjectStatus
    {
        /// <summary>
        /// Is the game object open in a prefab stage?
        /// </summary>
        public readonly bool IsInPrefabStage;

        /// <summary>
        /// What mode is the game object being edited in a prefab stage?
        /// </summary>
        public readonly PrefabStageMode PrefabStageMode;

        /// <summary>
        /// Is the game object <see cref="PrefabStage.prefabContentsRoot">the root object</see> in a prefab stage?
        /// </summary>
        public readonly bool IsRootOfPrefabStageHierarchy;

        /// <summary>
        /// Is the game object a prefab asset or open in a prefab stage?
        /// </summary>
        public readonly bool IsAsset;

        /// <summary>
        /// Is the game object a prefab variant asset, a prefab variant instance, or a prefab variant open in a prefab stage?
        /// </summary>
        public readonly bool IsVariant;

        /// <summary>
        /// Is the game object a prefab variant asset or a prefab variant open in a prefab stage?
        /// </summary>
        public readonly bool IsVariantAsset;

        /// <summary>
        /// Is the game object part of a prefab instance in a scene, and not open in a prefab stage?
        /// </summary>
        public readonly bool IsInstanceInScene;

        /// <summary>
        /// Is the game object a non-root object inside prefab instance?
        /// </summary>
        public readonly bool IsNestedInstanceInsideAnotherPrefab;

        /// <summary>
        /// Status about whether a prefab instance is properly connected to its asset.
        /// </summary>
        public readonly PrefabInstanceStatus PrefabInstanceStatus;

        /// <summary>
        /// Is the game object the root object in a prefab asset?
        /// </summary>
        public readonly bool IsRootOfAssetHierarchy;

        /// <summary>
        /// Is the game object a root object in a scene hierarchy, a prefab instance that is connected
        /// to the prefab asset, and not open in a prefab stage?
        /// </summary>
        public readonly bool IsRootOfInstanceHierarchy;

        /// <summary>
        /// Is the game object a root object in a scene hierarchy, not part of any prefab asset or prefab instance,
        /// and not open in a prefab stage?
        /// </summary>
        public readonly bool IsRootOfNonPrefabHierarchy;

        /// <summary>
        /// Is the game object the root of the nearest prefab instance?
        /// <para>
        /// The Transform hierarchy is searched until the root of any Prefab instance is found, regardless of whether that instance is an applied nested Prefab inside another Prefab, or not.
        /// </para>
        /// </summary>
        public readonly bool IsNearestPrefabInstanceRoot;

        /// <summary>
        /// Do any of the children (including nested children) of this game object contain a <see cref="CoherenceSync"/> component?
        /// <para>
        /// Components that don't have a <see cref="CoherenceSync.CoherenceSyncConfig"/> are not counted.
        /// </para>
        /// </summary>
        public readonly bool HasChildrenCoherenceSyncs;

        public GameObjectStatus(GameObject gameObject)
        {
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            IsInPrefabStage = prefabStage;
            PrefabStageMode = !IsInPrefabStage ? PrefabStageMode.None : prefabStage.mode == PrefabStage.Mode.InContext ? PrefabStageMode.InContext : PrefabStageMode.InIsolation;
            IsRootOfPrefabStageHierarchy = IsInPrefabStage && prefabStage.prefabContentsRoot == gameObject;
            PrefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
            IsAsset = PrefabUtility.IsPartOfPrefabAsset(gameObject) || IsInPrefabStage;
            IsVariant = PrefabUtility.IsPartOfVariantPrefab(gameObject);
            IsVariantAsset = IsAsset && IsVariant;
            IsInstanceInScene = PrefabUtility.IsPartOfPrefabInstance(gameObject) && !IsInPrefabStage &&
                                PrefabInstanceStatus != PrefabInstanceStatus.NotAPrefab;

            bool hasParentPrefabAsset;
#if UNITY_2022_2_OR_NEWER
            hasParentPrefabAsset = PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded(gameObject) != null;
#else
            hasParentPrefabAsset = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject) != null;
#endif

            var parent = gameObject.transform.parent;
            IsNestedInstanceInsideAnotherPrefab = PrefabUtility.IsPartOfPrefabInstance(gameObject) &&
                                                  (hasParentPrefabAsset || IsInPrefabStage) && parent;

            var isRoot = (!parent || parent.name.Equals("Canvas (Environment)"));
            IsRootOfAssetHierarchy = isRoot && !IsNestedInstanceInsideAnotherPrefab && (IsAsset || IsVariantAsset);
            IsRootOfInstanceHierarchy = isRoot && !IsNestedInstanceInsideAnotherPrefab &&
                                        (IsInstanceInScene && PrefabInstanceStatus == PrefabInstanceStatus.Connected);
            IsRootOfNonPrefabHierarchy = isRoot && !IsNestedInstanceInsideAnotherPrefab && !IsAsset &&
                                         !IsVariant && PrefabInstanceStatus == PrefabInstanceStatus.NotAPrefab;

            IsNearestPrefabInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) == gameObject;

            HasChildrenCoherenceSyncs = false;

            if (!IsRootOfAssetHierarchy)
            {
                return;
            }

            var children = gameObject.GetComponentsInChildren<CoherenceSync>(true);

            var isValidSyncHierarchy = false;
            foreach (var child in children)
            {
                if (child.transform.parent != parent && child.CoherenceSyncConfig)
                {
                    isValidSyncHierarchy = true;
                    break;
                }
            }

            HasChildrenCoherenceSyncs = isValidSyncHierarchy;
        }
    }
}
