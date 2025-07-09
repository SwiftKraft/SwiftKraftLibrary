// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    [DisplayName("Pool", "Instances of this prefab will be pooled.")]
    [Serializable]
    public class NetworkPool : INetworkObjectInstantiator, IDisposable
    {
        [SerializeField, Tooltip("Maximum number of objects held by this pool.")]
        private int maxSize = 40;

        [SerializeField, Tooltip("Initial number of objects that will be instantiated in the Start method of CoherenceBridge.")]
        private int initialSize = 0;

        private CoherenceObjectPool<ICoherenceSync> pool;
        private MonoBehaviour syncPrefab;
        private ICoherenceBridge lastBridge;
        private Vector3 position;
        private Quaternion rotation;
        private GameObject container;

        private bool warmingUp;

        public void OnUniqueObjectReplaced(ICoherenceSync instance) => pool.ForceGet(instance);

        public ICoherenceSync Instantiate(SpawnInfo spawnInfo)
        {
            lastBridge = spawnInfo.bridge;
            this.syncPrefab = spawnInfo.prefab as MonoBehaviour;
            this.position = spawnInfo.position;
            this.rotation = spawnInfo.rotation ?? Quaternion.identity;

            return pool.Get();
        }

        public void WarmUpInstantiator(CoherenceBridge bridge, CoherenceSyncConfig config, INetworkObjectProvider assetLoader)
        {
            InstantiatePool();
            InstantiateContainer(config.name);

            if (pool.CountAll >= initialSize)
            {
                return;
            }

            assetLoader.LoadAsset(config.ID, (prefab) => OnPrefabLoaded(bridge, prefab));
        }

        public static string GetContainerName(string assetName) => $"{assetName} (Pool)";
        public void Destroy(ICoherenceSync obj) => pool.Release(obj);
        public void OnApplicationQuit() => Dispose();

        private CoherenceSync CreatePooledItem()
        {
            var obj = Object.Instantiate(syncPrefab, position, rotation);

            if (!obj.gameObject.activeInHierarchy)
            {
                obj.transform.SetParent(container.transform);
            }

            return obj.GetComponent<CoherenceSync>();
        }

        private void OnReturnedToPool(ICoherenceSync sync)
        {
            if (!warmingUp)
            {
                var mb = sync as MonoBehaviour;
                mb.gameObject.SetActive(false);
                mb.transform.SetParent(container.transform);
            }
        }

        private bool OnTakeFromPool(ICoherenceSync sync)
        {
            var monoBehaviour = sync as MonoBehaviour;

            if (monoBehaviour == null)
            {
                return false;
            }

            var transform = monoBehaviour.transform;
            transform.position = position;
            transform.rotation = rotation;

            if (warmingUp)
            {
                return true;
            }

            monoBehaviour.gameObject.transform.SetParent(null);

            if (monoBehaviour.gameObject.scene != lastBridge.Scene)
            {
                SceneManager.MoveGameObjectToScene(monoBehaviour.gameObject, lastBridge.Scene);
            }

            monoBehaviour.gameObject.SetActive(true);

            return true;
        }

        private void OnDestroyPoolObject(ICoherenceSync sync)
        {
            var monoBehaviour = sync as MonoBehaviour;

            if (monoBehaviour != null)
            {
                Object.Destroy(monoBehaviour.gameObject);
            }
        }

        private void OnPrefabLoaded(CoherenceBridge bridge, ICoherenceSync prefab)
        {
            Scene? lastActiveScene = null;
            try
            {
                lastActiveScene = bridge.EntitiesManager.SetActiveScene();

                this.syncPrefab = prefab as MonoBehaviour;

                var lastActive = syncPrefab.gameObject.activeSelf;

                syncPrefab.gameObject.SetActive(false);

                var instances = new List<ICoherenceSync>();

                warmingUp = true;
                for (int i = 0; i < initialSize; i++)
                {
                    instances.Add(pool.Get());
                }

                foreach (var instance in instances)
                {
                    pool.Release(instance);
                }
                warmingUp = false;

                syncPrefab.gameObject.SetActive(lastActive);
            }
            finally
            {
                if (lastActiveScene.HasValue)
                {
                    SceneManager.SetActiveScene(lastActiveScene.Value);
                }
            }
        }

        private void InstantiateContainer(string assetName)
        {
            if (container == null)
            {
                container = new GameObject(GetContainerName(assetName));
                Object.DontDestroyOnLoad(container);
            }
        }

        private void InstantiatePool()
        {
            if (pool != null)
            {
                return;
            }

            pool = new CoherenceObjectPool<ICoherenceSync>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool,
                OnDestroyPoolObject,
                true, maxSize);
        }

        public void Dispose()
        {
            pool?.Dispose();
            pool = null;
        }
    }
}
