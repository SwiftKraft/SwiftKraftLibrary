// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using Common;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    [Preserve]
    internal class EntriesMigrator : IDataMigrator
    {
        public SemVersion MaxSupportedVersion => new SemVersion(2);
        public int Order => -100;
        public string MigrationMessage => "Created network object entries for CoherenceSync prefabs.";

        public void Initialize()
        {
            const string prefabMapperPath = "Assets/coherence/PrefabMapper.asset";
            if (File.Exists(prefabMapperPath))
            {
                AssetDatabase.DeleteAsset(prefabMapperPath);
            }
        }

        public IEnumerable<Object> GetMigrationTargets()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var guid in guids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab.TryGetComponent(out CoherenceSync sync))
                {
                    yield return sync;
                }
            }
        }

        public bool RequiresMigration(Object obj)
        {
            if (obj is not CoherenceSync sync)
            {
                return false;
            }

            return !CoherenceSyncConfigUtils.TryGetFromAsset(sync, out _);
        }

        public bool MigrateObject(Object obj)
        {
            if (obj is not CoherenceSync sync)
            {
                return false;
            }

            if (CoherenceSyncConfigUtils.TryGetFromAsset(sync, out _))
            {
                return false;
            }

            if (sync.CoherenceSyncConfig)
            {
                return CoherenceSyncConfigRegistry.Instance.Register(sync.CoherenceSyncConfig);
            }

            var config = CoherenceSyncConfigUtils.Create(obj);
            if (sync.CoherenceSyncConfig)
            {
                return true;
            }

            sync.CoherenceSyncConfig = config;
            EditorUtility.SetDirty(sync);

            return true;
        }
    }
}
