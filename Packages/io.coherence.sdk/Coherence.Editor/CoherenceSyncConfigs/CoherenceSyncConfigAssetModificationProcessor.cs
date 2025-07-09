// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;

    internal class CoherenceSyncConfigAssetModificationProcessor : AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions _)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == typeof(CoherenceSyncConfig))
            {
                var config = AssetDatabase.LoadAssetAtPath<CoherenceSyncConfig>(assetPath);
                OnWillDeleteConfig(config);
            }
            else if (assetType == typeof(GameObject))
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                OnWillDeletePrefab(guid);
            }

            return AssetDeleteResult.DidNotDelete;
        }

        private static void OnWillDeleteConfig(CoherenceSyncConfig config)
        {
            var registry = CoherenceSyncConfigRegistry.Instance;
            if (registry.IsLeaked(config))
            {
                registry.RemoveLeaked(config);
            }

            _ = registry.Deregister(config);

            if (config && config.IsLinked)
            {
                // only delete components on the prefab when the CoherenceSyncConfig is properly linked
                CoherenceSyncUtils.DestroyCoherenceComponents((GameObject)config.EditorTarget);
            }
        }

        private static void OnWillDeletePrefab(string guid)
        {
            var registry = CoherenceSyncConfigRegistry.Instance;
            if (!registry.TryGetFromAssetId(guid, out var config))
            {
                return;
            }

            registry.Deregister(config);
            CoherenceSyncConfigUtils.Delete(config);
        }
    }
}
