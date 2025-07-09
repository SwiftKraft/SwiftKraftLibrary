namespace Coherence.Editor
{
    using System;
    using System.IO;
    using Coherence.Toolkit;
    using UnityEditor;
#if HAS_ADDRESSABLES
    using UnityEditor.AddressableAssets;
#endif
    using UnityEditor.Callbacks;
    using UnityEngine;

    internal class CoherenceSyncConfigPostprocessor : AssetPostprocessor
    {
        [RunAfterClass(typeof(CoherenceSyncConfigRegistryPostprocessor))]
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (CloneMode.Enabled)
            {
                return;
            }

            foreach (var importedAsset in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(importedAsset) != typeof(CoherenceSyncConfig))
                {
                    continue;
                }

                var config = AssetDatabase.LoadAssetAtPath<CoherenceSyncConfig>(importedAsset);
                OnImportConfig(config);
            }

            foreach (var deletedAsset in deletedAssets)
            {
                var guid = AssetDatabase.AssetPathToGUID(deletedAsset);
                if (!CoherenceSyncConfigRegistry.Instance.TryGetSyncFromConfigId(guid, out var sync))
                {
                    continue;
                }

                var syncPath = AssetDatabase.GetAssetPath(sync);
                var syncGuid = AssetDatabase.AssetPathToGUID(syncPath);
                _ = CoherenceSyncConfigRegistry.Instance.Deregister(syncGuid);
                CoherenceSyncUtils.DestroyCoherenceComponents(sync.gameObject);
            }

            foreach (var moveAsset in movedAssets)
            {
                if (!TryGetCoherenceSync(moveAsset, out var sync))
                {
                    continue;
                }

                var configAssetPath = AssetDatabase.GetAssetPath(sync.CoherenceSyncConfig);
                var assetFileName = Path.GetFileNameWithoutExtension(moveAsset);
                var configFileName = Path.GetFileNameWithoutExtension(configAssetPath);
                if (assetFileName.Equals(configFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var configAssetFolder = Path.GetDirectoryName(configAssetPath);
                var newConfigAssetPath = Path.Combine(configAssetFolder, $"{assetFileName}.asset");
                _ = AssetUtils.MoveFile(configAssetPath, newConfigAssetPath);
            }
        }

        private static bool TryGetCoherenceSync(string assetPath, out CoherenceSync sync)
        {
            var loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!loadedPrefab)
            {
                sync = null;
                return false;
            }

            sync = loadedPrefab.GetComponent<CoherenceSync>();
            return (bool)sync;
        }

        private static void OnImportConfig(CoherenceSyncConfig config)
        {
            config.UpdateSelfId();
            CoherenceSyncConfigRegistry.Instance.Register(config);

#if HAS_ADDRESSABLES
            if (config.Provider is not AddressablesProvider)
            {
                return;
            }

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (!addressableSettings)
            {
                return;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(config.EditorTarget, out var guid, out long _))
            {
                return;
            }

            var addressableEntry = addressableSettings.FindAssetEntry(guid);
            if (addressableEntry == null)
            {
                addressableSettings.CreateOrMoveEntry(guid, addressableSettings.DefaultGroup);
            }
#endif
        }
    }
}

