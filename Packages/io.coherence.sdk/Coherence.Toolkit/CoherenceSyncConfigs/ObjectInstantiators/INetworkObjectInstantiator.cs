// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    public interface INetworkObjectInstantiator
    {
        /// <summary>
        ///     Called when a unique object instance was replaced.
        /// </summary>
        void OnUniqueObjectReplaced(ICoherenceSync instance);

        /// <summary>
        ///     Called when coherence needs to instantiate a CoherenceSync prefab, to link it to a new network entity.
        /// </summary>
        ICoherenceSync Instantiate(SpawnInfo spawnInfo);

        /// <summary>
        ///     Called from the Start method of a given CoherenceBridge instance. The related Provider is also given to be able to
        ///     load the Object if you wish to do so. Consider deactivating the prefab before instantiating it if you wish to initiate an Object Pool.
        /// </summary>
        void WarmUpInstantiator(CoherenceBridge bridge, CoherenceSyncConfig config, INetworkObjectProvider assetLoader);

        /// <summary>
        ///     Called when coherence needs to destroy a prefab instance, when the related network entity is destroyed.
        /// </summary>
        void Destroy(ICoherenceSync obj);

        /// <summary>
        ///     Called when the application exits.
        /// </summary>
        void OnApplicationQuit();
    }
}
