// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using Log;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class CoherenceSyncExtensions
    {
        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with coherence (via a CoherenceBridge).
        /// </summary>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public static CoherenceSync GetInstance(this CoherenceSync sync)
        {
            return sync.GetInstance(SceneManager.GetActiveScene());
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the selected scene if its networked with coherence (via a CoherenceBridge).
        /// </summary>
        /// <param name="scene">Scene that has a CoherenceBridge to synchronize it with.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public static CoherenceSync GetInstance(this CoherenceSync sync, Scene scene)
        {
            if (!CoherenceBridgeStore.TryGetBridge(scene, out var bridge))
            {
                sync.logger.Warning(Warning.ToolkitSyncSceneMissingBridge,
                    ("prefab", sync.name));

                return null;
            }

            return sync.GetInstance(bridge, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with coherence (via a CoherenceBridge).
        /// </summary>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public static CoherenceSync GetInstance(this CoherenceSync sync, Vector3 position, Quaternion rotation)
        {
            return sync.GetInstance(SceneManager.GetActiveScene(), position, rotation);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the selected scene if its networked with coherence (via a CoherenceBridge).
        /// </summary>
        /// <param name="scene">Scene that has a CoherenceBridge to synchronize it with.</param>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public static CoherenceSync GetInstance(this CoherenceSync sync, Scene scene, Vector3 position, Quaternion rotation)
        {
            if (!CoherenceBridgeStore.TryGetBridge(scene, out var bridge))
            {
                sync.logger.Warning(Warning.ToolkitSyncSceneMissingBridge,
                    ("prefab", sync.name));

                return null;
            }

            return sync.GetInstance(bridge, position, rotation);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with coherence (via a CoherenceBridge).
        /// </summary>
        /// <param name="bridge">CoherenceBridge that will handle networking this prefab instance.</param>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public static CoherenceSync GetInstance(this CoherenceSync sync, ICoherenceBridge bridge, Vector3 position, Quaternion rotation)
        {
            var spawnData = new SpawnInfo()
            {
                bridge = bridge,
                position = position,
                rotation = rotation,
                prefab = sync
            };
            return sync.CoherenceSyncConfig.Instantiator.Instantiate(spawnData) as CoherenceSync;
        }

        /// <summary>
        ///     Use this method to destroy a CoherenceSync instance that was fetched with the GetInstance methods.
        /// </summary>
        public static void ReleaseInstance(this CoherenceSync sync)
        {
            sync.CoherenceSyncConfig.Instantiator.Destroy(sync);
        }

        public static CoherenceSync.AuthorityTransferType MapToType(this CoherenceSync.AuthorityTransferConfig config)
        {
            return config switch
            {
                CoherenceSync.AuthorityTransferConfig.Default => (CoherenceSync.AuthorityTransferType)RuntimeSettings.Instance.defaultAuthorityTransferType,
                CoherenceSync.AuthorityTransferConfig.NotTransferable => CoherenceSync.AuthorityTransferType.NotTransferable,
                CoherenceSync.AuthorityTransferConfig.Request => CoherenceSync.AuthorityTransferType.Request,
                CoherenceSync.AuthorityTransferConfig.Stealing => CoherenceSync.AuthorityTransferType.Stealing,
                _ => throw new System.Exception($"Unknown authority transfer config: {config}"),
            };
        }

        public static CoherenceSync.AuthorityTransferConfig MapToConfig(this CoherenceSync.AuthorityTransferType type)
        {
            return type switch
            {
                CoherenceSync.AuthorityTransferType.NotTransferable => CoherenceSync.AuthorityTransferConfig.NotTransferable,
                CoherenceSync.AuthorityTransferType.Request => CoherenceSync.AuthorityTransferConfig.Request,
                CoherenceSync.AuthorityTransferType.Stealing => CoherenceSync.AuthorityTransferConfig.Stealing,
                _ => throw new System.Exception($"Unknown authority transfer type: {type}"),
            };
        }
    }
}
