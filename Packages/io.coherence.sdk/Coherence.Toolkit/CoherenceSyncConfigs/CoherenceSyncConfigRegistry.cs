// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine.Profiling;
#endif

    /// <summary>
    /// Holds references to <see cref="CoherenceSyncConfig"/>s.
    /// Network entities known to coherence are registered as part of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While on the Editor, registered entries are loaded and serialized on memory but not serialized to disk.
    /// This means <see cref="CoherenceSyncConfigRegistry"/> assets should not show changes on disk when it comes to
    /// the network entities being tracked.
    /// </para>
    /// <para>
    /// When building, an internal implementation of <see cref="UnityEditor.Build.IPreprocessBuildWithReport"/>
    /// serializes the changes to disk.
    /// </para>
    /// <para>
    /// Once the build is completed, an internal implementation of
    /// <see cref="UnityEditor.Build.IPostprocessBuildWithReport"/> clears the changes previously made to the file.
    /// </para>
    /// <para>
    /// This post-processing might update the file's modification date.
    /// </para>
    /// </remarks>
    [PreloadedSingleton]
    public sealed class CoherenceSyncConfigRegistry : PreloadedSingleton<CoherenceSyncConfigRegistry>, IEnumerable<CoherenceSyncConfig>
    {
#if UNITY_EDITOR
        private const string ConfigSearchFilter = "t:" + nameof(CoherenceSyncConfig);
        private static readonly string[] ConfigSearchFolders =
        {
            "Assets/coherence/CoherenceSyncConfigs",
        };
#endif

        /// <summary>
        /// List of CoherenceSyncConfigs that are serialized as part of the registry.
        /// This list gets filled at build-time, so that at runtime it can be reconstructed
        /// without the help of editor APIs.
        /// </summary>
        [SerializeField] private List<CoherenceSyncConfig> storedConfigs = new();
        private readonly List<CoherenceSyncConfig> configs = new();
        // Key: CoherenceSync asset GUID
        // Value: Associated CoherenceSyncConfig
        private readonly Dictionary<string, CoherenceSyncConfig> syncGuidToConfigDictionary = new();

        // Key: int via CoherenceSync asset GUID.GetHashCode()
        // Value: Associated CoherenceSyncConfig
        // This is another way of accessing the same configs as above except the ID is fewer bytes
        // than the GUID so it can be sent on the network and not waste bandwidth.
        private readonly Dictionary<int, CoherenceSyncConfig> networkIdToConfigDictionary = new();

        // Key: CoherenceSyncConfig asset GUID
        // Value: Associated CoherenceSync
        // When CoherenceSyncConfigs are deleted outside of Unity, there is no way to read any
        // of their data, so it's not possible to find the associated CoherenceSync and update it accordingly.
        // This storage helps keeps this connection in memory, which is picked up by the postprocessors.
        // See CoherenceSyncConfigPostprocessor.
#if UNITY_EDITOR
        private readonly Dictionary<string, CoherenceSync> configGuidToSyncDictionary = new();

        /// <summary>
        /// List of CoherenceSyncConfigs that try to get registered but can't e.g., duplicates.
        /// </summary>
        private readonly List<CoherenceSyncConfig> leakedConfigs = new();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RuntimeInitialize() => Instance.RegisterStored();

        /// <summary>
        /// The number of <see cref="CoherenceSyncConfig"/>s registered.
        /// </summary>
        public int Count => configs.Count;

#if UNITY_EDITOR
        /// <seealso cref="GetLeakedAt"/>
        internal int LeakedCount => leakedConfigs.Count;

        /// <summary>
        /// <see cref="CoherenceSyncConfig"/> assets that are present in the project but
        /// were not registered as part of <see cref="ReimportConfigs"/>.
        /// <seealso cref="LeakedCount"/>
        /// </summary>
        internal CoherenceSyncConfig GetLeakedAt(int index) => leakedConfigs[index];

        /// <see cref="RemoveLeaked"/>
        internal bool IsLeaked(CoherenceSyncConfig config) => leakedConfigs.Contains(config);

        /// <see cref="IsLeaked"/>
        internal bool RemoveLeaked(CoherenceSyncConfig config) => leakedConfigs.Remove(config);
#endif

        /// <summary>
        /// Gets the <see cref="CoherenceSyncConfig"/> registered at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <para><paramref name="index"/> is less than 0</para>
        /// <para>-or-</para>
        /// <para><paramref name="index"/> is equal or greater than <see cref="Count"/></para>
        /// </exception>
        public CoherenceSyncConfig GetAt(int index) => configs[index];

        /// <returns>
        /// Returns an enumerator that iterates through the collection of registered <see cref="CoherenceSyncConfig"/>s.
        /// </returns>
        public List<CoherenceSyncConfig>.Enumerator GetEnumerator() => configs.GetEnumerator();

        IEnumerator<CoherenceSyncConfig> IEnumerable<CoherenceSyncConfig>.GetEnumerator() => configs.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => configs.GetEnumerator();

        internal void Store()
        {
            storedConfigs.Clear();
            storedConfigs.AddRange(configs);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        internal void ClearStore()
        {
            if (storedConfigs.Count == 0)
            {
                return;
            }

            storedConfigs.Clear();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private void RegisterStored()
        {
            foreach (var config in storedConfigs)
            {
                Register(config);
            }
        }

        internal bool Register(CoherenceSyncConfig config)
        {
            if (!config)
            {
                return false;
            }

            if (string.IsNullOrEmpty(config.ID))
            {
                return false;
            }

            // Data is stored in 3 structures, but we only check in one of the dictionaries
            if (syncGuidToConfigDictionary.ContainsKey(config.ID))
            {
                return false;
            }

            var configHash = config.ID.GetHashCode();

            if (networkIdToConfigDictionary.ContainsKey(configHash))
            {
                // this is a serious problem.
                return false;
            }

            configs.Add(config);
            syncGuidToConfigDictionary.Add(config.ID, config);
            networkIdToConfigDictionary.Add(configHash, config);
#if UNITY_EDITOR
            // SubAssets have no own GUID - they are part of the Registry asset
            if (!string.IsNullOrEmpty(config.SelfID) &&
                config.EditorTarget is GameObject gameObject &&
                gameObject.TryGetComponent(out CoherenceSync sync))
            {
                configGuidToSyncDictionary[config.SelfID] = sync;
            }
#endif
            return true;
        }

        internal bool Deregister(CoherenceSyncConfig config)
        {
            return config && Deregister(config.ID);
        }

        internal bool Deregister(string syncGuid)
        {
            if (string.IsNullOrEmpty(syncGuid))
            {
                return false;
            }

            if (!syncGuidToConfigDictionary.TryGetValue(syncGuid, out var config))
            {
                return false;
            }

            var removed = true;
            removed &= syncGuidToConfigDictionary.Remove(config.ID);
            removed &= networkIdToConfigDictionary.Remove(config.ID.GetHashCode());
            removed &= configs.Remove(config);
#if UNITY_EDITOR
            // this dictionary won't contain an entry for the config if it is a subasset,
            // so we don't use it to assert proper removal
            if (!string.IsNullOrEmpty(config.SelfID))
            {
                _ = configGuidToSyncDictionary.Remove(config.SelfID);
            }
#endif
            Debug.Assert(removed);
            return removed;
        }

        private void DeregisterAll()
        {
            configs.Clear();
            syncGuidToConfigDictionary.Clear();
            networkIdToConfigDictionary.Clear();
#if UNITY_EDITOR
            configGuidToSyncDictionary.Clear();
            leakedConfigs.Clear();
#endif
        }

        internal void WarmUp(CoherenceBridge bridge)
        {
            foreach (var config in configs)
            {
                if (!config)
                {
                    continue;
                }

                config.Instantiator.WarmUpInstantiator(bridge, config, config.Provider);
            }
        }

        // TODO
        // This looks like an Editor-only functionality that could trigger on leaving Play Mode via PlayModeStateChange.
        // Another completely different approach would be to use [RuntimeInitializeOnLoadMethodAttribute] to let
        // users clean-up state at an initialization stage, similar to how static data has to be reset when domain
        // reloads are disabled (Enter Play Mode Options): https://docs.unity3d.com/Manual/DomainReloading.html
        internal void CleanUp()
        {
            foreach (var config in configs)
            {
                config.Instantiator.OnApplicationQuit();
                config.Provider.OnApplicationQuit();
            }
        }

        /// <summary>
        /// Gets the <see cref="CoherenceSyncConfig"/> associated with the specified identifier.
        /// </summary>
        /// <param name="assetId">The <see cref="CoherenceSyncConfig.ID"/>
        /// of the <see cref="CoherenceSyncConfig"/> to retrieve.</param>
        /// <param name="config">When this method returns, contains the <see cref="CoherenceSyncConfig"/> identified
        /// with the specified <paramref name="assetId"/>, if registered; otherwise, the default value for
        /// <see cref="CoherenceSyncConfig"/>. This parameter is passed uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="assetId"/> is registered
        /// in the <see cref="CoherenceSyncConfigRegistry"/>.
        /// </returns>
        public bool TryGetFromAssetId(string assetId, out CoherenceSyncConfig config)
        {
            return syncGuidToConfigDictionary.TryGetValue(assetId, out config);
        }

        /// <summary>
        ///     Gets the <see cref="CoherenceSyncConfig"/> associated with the specified identifier.
        /// </summary>
        /// <remarks>
        ///     This is intended for internal use for serializing assets across the network.
        /// </remarks>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="networkId"/> is registered
        ///     in the <see cref="CoherenceSyncConfigRegistry"/>.
        /// </returns>
        public bool GetFromNetworkId(int networkId, out CoherenceSyncConfig config)
        {
            return networkIdToConfigDictionary.TryGetValue(networkId, out config);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gets the <see cref="CoherenceSync"/> associated with the specified identifier.
        /// </summary>
        /// <param name="configGuid">The <see cref="CoherenceSyncConfig.SelfID"/>
        /// of the <see cref="CoherenceSyncConfig"/> to retrieve.</param>
        /// <param name="sync">When this method returns, contains the <see cref="CoherenceSync"/> associated
        /// with the specified <paramref name="configGuid"/>, if registered; otherwise, the default value for
        /// <see cref="CoherenceSync"/>. This parameter is passed uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="configGuid"/> is registered
        /// in the <see cref="CoherenceSyncConfigRegistry"/>.
        /// </returns>
        internal bool TryGetSyncFromConfigId(string configGuid, out CoherenceSync sync)
        {
            return configGuidToSyncDictionary.TryGetValue(configGuid, out sync);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!this)
            {
                return;
            }

            hideFlags &= HideFlags.NotEditable;

            ReimportConfigs();

            // In edge cases, CoherenceSyncConfigRegistryBuildPreprocessor might not clean up the stored configs.
            // This makes sure that upon recompiling, at singleton activation time those references are removed
            // from the serialized data, and the asset is saved to disk.
            ClearStore();
            AssetDatabase.SaveAssetIfDirty(this);
        }

        internal void ReimportConfigs()
        {
            DeregisterAll();

            // We supported having configs be subobjects of the registry. Instead of trying to migrate this right away,
            // we keep loading them. This ensures references are kept, and it's up to the user to extract them at their
            // convenience.
            // We don't support creating configs as subobjects anymore.

            var assetPath = AssetDatabase.GetAssetPath(this);
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (var subAsset in subAssets)
            {
                if (subAsset is not CoherenceSyncConfig config)
                {
                    continue;
                }

                if (!Register(config))
                {
                    leakedConfigs.Add(config);
                }
            }

            if (!AssetDatabase.IsValidFolder(ConfigSearchFolders[0]))
            {
                return;
            }

            Profiler.BeginSample("Find CoherenceSyncConfigs");
            var guids = AssetDatabase.FindAssets(ConfigSearchFilter, ConfigSearchFolders);
            Profiler.EndSample();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<CoherenceSyncConfig>(path);
                if (!config)
                {
                    continue;
                }

                if (!Register(config))
                {
                    leakedConfigs.Add(config);
                }
            }
        }

        internal void GetReferencedPrefabs(List<CoherenceSync> results)
        {
            results.Clear();
            foreach (var config in configs)
            {
                if (config.EditorTarget is not GameObject gameObject)
                {
                    continue;
                }

                if (gameObject.TryGetComponent(out CoherenceSync sync))
                {
                    results.Add(sync);
                }
            }
        }
#endif
    }
}
