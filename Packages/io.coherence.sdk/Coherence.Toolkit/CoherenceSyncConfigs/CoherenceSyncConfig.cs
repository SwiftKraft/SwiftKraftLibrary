// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using Log;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;
    using Logger = Log.Logger;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public sealed class CoherenceSyncConfig : ScriptableObject
    {
        public bool IncludeInSchema
        {
            get => includeInSchema;
            set => includeInSchema = value;
        }

        public string ID => id;
        internal string SelfID => selfId;

        public INetworkObjectProvider Provider
        {
            get => objectProvider;
            internal set => objectProvider = value;
        }

        public INetworkObjectInstantiator Instantiator
        {
            get => objectInstantiator;
            internal set => objectInstantiator = value;
        }

#if UNITY_EDITOR
        public Object EditorTarget => editorTarget;

        public CoherenceSync Sync => (EditorTarget &&
                                     EditorTarget is GameObject gameObject &&
                                     gameObject.TryGetComponent(out CoherenceSync sync)) ? sync : default;

        public bool IsLinked => Sync is { } sync && sync.CoherenceSyncConfig == this;
#endif

        [SerializeField]
        private string id;

        [SerializeField]
        private string selfId;

#if UNITY_EDITOR
        [SerializeField]
        [ReadOnly(false)]
        private Object editorTarget;
#endif

        [SerializeField]
        [Tooltip(
            "If disabled, this Object will not be included in the Schema and it will not be synchronized with the network.")]
        private bool includeInSchema = true;

        [SerializeReference]
        [ObjectProvider(typeof(INetworkObjectProvider), "Load via",
            "Choose how coherence will load this prefab to memory when a new remote entity spawns in the network. You can hook your own implementation using the INetworkObjectProvider interface.")]
        private INetworkObjectProvider objectProvider = new DirectReferenceProvider();

        [SerializeReference]
        [ObjectProvider(typeof(INetworkObjectInstantiator), "Instantiate via",
            "Choose how coherence will instantiate this prefab into the scene when a new remote entity spawns in the network. You can hook your own implementation using the INetworkObjectInstantiator interface.")]
        private INetworkObjectInstantiator objectInstantiator = new DefaultInstantiator();

        private Logger logger = Log.GetLogger<CoherenceSyncConfig>();

        private void OnDestroy()
        {
            var registry = CoherenceSyncConfigRegistry.Instance;
            if (registry)
            {
                registry.Deregister(this);
            }

            if (objectInstantiator is IDisposable disposableObjectInstantiator)
            {
                disposableObjectInstantiator.Dispose();
            }
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab needs to be loaded asynchronously through Addressables or an Asset Bundle.
        /// </summary>
        /// <param name="onInstantiate">Callback that will be invoked with the result of the instantiation.</param>
        public void GetInstanceAsync(Action<CoherenceSync> onInstantiate)
        {
            GetInstanceAsync(SceneManager.GetActiveScene(), onInstantiate);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the selected scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab needs to be loaded asynchronously through Addressables or an Asset Bundle.
        /// </summary>
        /// <param name="scene">Scene that has a CoherenceBridge to synchronize it with.</param>
        /// <param name="onInstantiate">Callback that will be invoked with the result of the instantiation.</param>
        public void GetInstanceAsync(Scene scene, Action<CoherenceSync> onInstantiate)
        {
            if (!CoherenceBridgeStore.TryGetBridge(scene, out var bridge))
            {
                logger.Warning(Warning.ToolkitSyncSceneMissingBridge,
                    ("prefab", name));

                onInstantiate?.Invoke(null);
                return;
            }

            GetInstanceAsync(bridge, Vector3.zero, Quaternion.identity, onInstantiate);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab needs to be loaded asynchronously through Addressables or an Asset Bundle.
        /// </summary>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <param name="onInstantiate">Callback that will be invoked with the result of the instantiation.</param>
        public void GetInstanceAsync(Vector3 position, Quaternion rotation, Action<CoherenceSync> onInstantiate)
        {
            GetInstanceAsync(SceneManager.GetActiveScene(), position, rotation, onInstantiate);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the selected scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab needs to be loaded asynchronously through Addressables or an Asset Bundle.
        /// </summary>
        /// <param name="scene">Scene that has a CoherenceBridge to synchronize it with.</param>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <param name="onInstantiate">Callback that will be invoked with the result of the instantiation.</param>
        public void GetInstanceAsync(Scene scene, Vector3 position, Quaternion rotation,
            Action<CoherenceSync> onInstantiate)
        {
            if (!CoherenceBridgeStore.TryGetBridge(scene, out var bridge))
            {
                logger.Warning(Warning.ToolkitSyncSceneMissingBridge,
                    ("prefab", name));

                onInstantiate?.Invoke(null);
                return;
            }

            GetInstanceAsync(bridge, position, rotation, onInstantiate);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the scene that the CoherenceBridge is
        ///     attached to.
        ///     Use this method when the prefab needs to be loaded through Addressables or an Asset Bundle.
        /// </summary>
        /// <param name="bridge">CoherenceBridge that will handle networking this prefab instance.</param>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <param name="onInstantiate">Callback that will be invoked with the result of the instantiation.</param>
        public void GetInstanceAsync(ICoherenceBridge bridge, Vector3 position, Quaternion rotation, Action<CoherenceSync> onInstantiate)
        {
            if (!AssertProviderInstantiatorPresent())
            {
                onInstantiate?.Invoke(null);
                return;
            }

            objectProvider.LoadAsset(id, sync =>
            {
                if (sync == null)
                {
                    onInstantiate?.Invoke(null);
                    return;
                }

                var instance = GetInstance(bridge, position, rotation, sync);
                onInstantiate?.Invoke(instance);
            });
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab is loaded via an Object Provider that has a synchronous implementation.
        ///     Use with care when loading the prefab can incur into heavy performance penalties if the prefab is loaded
        ///     through an Asset Bundle.
        /// </summary>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public CoherenceSync GetInstance()
        {
            return GetInstance(SceneManager.GetActiveScene());
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the selected scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab is loaded via an Object Provider that has a synchronous implementation.
        ///     Use with care when loading the prefab can incur into heavy performance penalties if the prefab is loaded
        ///     through an Asset Bundle.
        /// </summary>
        /// <param name="scene">Scene that has a CoherenceBridge to synchronize it with.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public CoherenceSync GetInstance(Scene scene)
        {
            if (!CoherenceBridgeStore.TryGetBridge(scene, out var bridge))
            {
                logger.Warning(Warning.ToolkitSyncSceneMissingBridge,
                    ("prefab", name));

                return null;
            }

            return GetInstance(bridge, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab is loaded via an Object Provider that has a synchronous implementation.
        ///     Use with care when loading the prefab can incur into heavy performance penalties if the prefab is loaded
        ///     through an Asset Bundle.
        /// </summary>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public CoherenceSync GetInstance(Vector3 position, Quaternion rotation)
        {
            return GetInstance(SceneManager.GetActiveScene(), position, rotation);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the selected scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab is loaded via an Object Provider that has a synchronous implementation.
        ///     Use with care when loading the prefab can incur into heavy performance penalties if the prefab is loaded
        ///     through an Asset Bundle.
        /// </summary>
        /// <param name="scene">Scene that has a CoherenceBridge to synchronize it with.</param>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public CoherenceSync GetInstance(Scene scene, Vector3 position, Quaternion rotation)
        {
            if (!CoherenceBridgeStore.TryGetBridge(scene, out var bridge))
            {
                logger.Warning(Warning.ToolkitSyncSceneMissingBridge,
                    ("prefab", name));

                return null;
            }

            return GetInstance(bridge, position, rotation);
        }

        /// <summary>
        ///     Use the CoherenceSynConfig instantiator to get a CoherenceSync instance in the active scene if its networked with
        ///     coherence (via a CoherenceBridge).
        ///     Use this method when the prefab is loaded via an Object Provider that has a synchronous implementation.
        ///     Use with care when loading the prefab can incur into heavy performance penalties if the prefab is loaded
        ///     through an Asset Bundle.
        /// </summary>
        /// <param name="bridge">CoherenceBridge that will handle networking this prefab instance.</param>
        /// <param name="position">Position where the prefab will be instantiated.</param>
        /// <param name="rotation">Rotation with which the prefab will be instantiated.</param>
        /// <returns>Returns an instance of the CoherenceSync prefab, if it was instantiated successfully.</returns>
        public CoherenceSync GetInstance(ICoherenceBridge bridge, Vector3 position, Quaternion rotation)
        {
            if (!AssertProviderInstantiatorPresent())
            {
                return null;
            }

            var sync = objectProvider.LoadAsset(id);

            return sync != null ? GetInstance(bridge, position, rotation, sync) : null;
        }

        /// <summary>
        ///     Use this method to destroy CoherenceSync instances that have been loaded via the Instantiate methods of
        ///     CoherenceSyncConfigManager.
        /// </summary>
        public void ReleaseInstance(CoherenceSync instance)
        {
            objectInstantiator?.Destroy(instance);
        }

        private CoherenceSync GetInstance(ICoherenceBridge bridge, Vector3 position, Quaternion rotation, ICoherenceSync sync)
        {
            var spawnData = new SpawnInfo
            {
                bridge = bridge,
                position = position,
                rotation = rotation,
                prefab = sync,
            };
            var syncInstance = objectInstantiator.Instantiate(spawnData) as CoherenceSync;

            if (syncInstance != null)
            {
                syncInstance.loadedViaCoherenceSyncConfig = true;
            }

            return syncInstance;
        }

        private bool AssertProviderInstantiatorPresent()
        {
            if (objectProvider != null && objectInstantiator != null)
            {
                return true;
            }

            logger.Warning(Warning.ToolkitSyncMissingProvider,
                ("prefab", name));

            return false;
        }

#if UNITY_EDITOR
        internal void Init(Object obj)
        {
            id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            editorTarget = obj;
            name = obj.name;
            EditorUtility.SetDirty(this);
        }

        internal void UpdateSelfId()
        {
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
            if (selfId == guid)
            {
                return;
            }

            selfId = guid;
            EditorUtility.SetDirty(this);
        }
#endif

        internal void Init(string identifier)
        {
            id = identifier;
        }
    }
}
