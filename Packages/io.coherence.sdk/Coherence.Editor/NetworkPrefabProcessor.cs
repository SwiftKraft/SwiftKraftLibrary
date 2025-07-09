// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using Log;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;
    using Logger = Log.Logger;

    internal class NetworkPrefabProcessor
    {
        private static readonly Logger logger = Log.GetLogger<NetworkPrefabProcessor>();

        internal static bool UpdateNetworkPrefabs(List<CoherenceSync> prefabs, List<CoherenceSync> prefabsChanged)
        {
#if COHERENCE_DONT_UPDATE_PREFABS
            return false;
#else
            if (CloneMode.Enabled)
            {
                return false;
            }

            if (EditorApplication.isCompiling)
            {
                // We can't update prefabs serialized data when in the middle of the compilation loop
                return false;
            }

            if (prefabs == null)
            {
                logger.Error(Error.EditorNetworkPrefabProcessorUpdateNullPrefab);
                return false;
            }

            var updatedAnyPrefab = false;
            foreach (var prefab in prefabs)
            {
                if (!prefab)
                {
                    continue;
                }

                if (PrefabUtility.IsPartOfAnyPrefab(prefab.gameObject) &&
                    !PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(prefab.gameObject))
                {
                    logger.Warning(Warning.EditorNetworkPrefabProcessorUpdateMissingScripts,
                        $"Failed to update prefab '{AssetDatabase.GetAssetPath(prefab)}'.\nIt might contain missing scripts.");
                    continue;
                }

                var result = Migration.Migrate(prefab);
                var changed = false;
                changed |= result.Item1;
                changed |= TryUpdatePrefab(prefab);
                if (changed)
                {
                    prefabsChanged.Add(prefab);
                }

                updatedAnyPrefab |= changed;
            }

            if (updatedAnyPrefab)
            {
                BakeUtil.CoherenceSyncSchemasDirty = true;
            }

            return updatedAnyPrefab;
#endif
        }

        private static bool HasMissingScripts(GameObject go)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(go))
            {
                return false;
            }

            if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(go))
            {
                logger.Error(Error.EditorNetworkPrefabProcessorHasMissingScripts,
                    $"Failed to update prefab '{AssetDatabase.GetAssetPath(go)}'.\nIt might contain missing scripts.");
            }

            var allComponents = go.GetComponentsInChildren<Component>();

            foreach (var component in allComponents)
            {
                if (component == null)
                {
                    logger.Error(Error.EditorNetworkPrefabProcessorHasMissingScripts,
                        $"Tried to update Network Prefab {go.name} but it has missing scripts.");
                    return true;
                }
            }

            return false;
        }

        public static bool TryUpdatePrefab(CoherenceSync sync)
        {
#if COHERENCE_DONT_UPDATE_PREFABS
            return false;
#else
            if (!sync)
            {
                return false;
            }

            var updated = false;

            using var so = new SerializedObject(sync);

            var t = CoherenceSyncEditor.GetBakedScriptFullTypeName(sync);
            if (!string.IsNullOrEmpty(t))
            {
                using var bakedScriptType = so.FindProperty("bakedScriptType");
                bakedScriptType.stringValue = t;
                using var prefabInstanceUUID = so.FindProperty("scenePrefabInstanceUUID");
                prefabInstanceUUID.stringValue = null;

                if (so.ApplyModifiedProperties())
                {
                    updated = true;
                }
            }

            return updated;
#endif
        }
    }
}
