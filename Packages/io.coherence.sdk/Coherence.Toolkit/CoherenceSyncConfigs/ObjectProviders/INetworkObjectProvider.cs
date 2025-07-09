// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;

    public interface INetworkObjectProvider
    {
        /// <summary>
        ///     Called when coherence needs to load a prefab to memory, to instantiate it when a new network entity
        ///     is created.
        /// </summary>
        void LoadAsset(string networkAssetId, Action<ICoherenceSync> onLoaded);
        /// <summary>
        ///     Called from CoherenceSyncConfig when CoherenceSyncConfig.Instantiate is called, the object is loaded forcing
        ///     synchronicity. If the provider is naturally asynchronous (Addressables/Asset Bundles), using this method
        ///     can incur in heavy performance penalties because the main thread will be blocked while the object is loaded.
        /// </summary>
        ICoherenceSync LoadAsset(string networkAssetId);
        /// <summary>
        ///     Called when coherence needs to destroy a prefab instance, when the related network entity is destroyed.
        /// </summary>
        void Release(ICoherenceSync obj);
        /// <summary>
        ///     Called when the application exits.
        /// </summary>
        void OnApplicationQuit();
#region UNITY_EDITOR
        /// <summary>
        ///     Editor only method, that is called when an instance of this provider is assigned to a network entry.
        ///     If Validate returns false, the UI will show a button that will call Initialize again.
        /// </summary>
        void Initialize(CoherenceSyncConfig entry);
        /// <summary>
        ///     Editor only method, that is called by the UI to validate the serialized data of this Provider.
        /// </summary>
        bool Validate(CoherenceSyncConfig entry);
#endregion
    }
}

