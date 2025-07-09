// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [DisplayName("DestroyCoherenceSync", "Instances of this prefab will be instantiated normally, but only the CoherenceSync component will be destroyed or disabled." +
                                     "Use it when you want to keep the GameObject to be reused or destroyed manually.")]
    [Serializable]
    public class CoherenceSyncInstantiator : INetworkObjectInstantiator
    {
        [SerializeField, Tooltip("Choose what you want to happen when Destroy is called:\n\nDestroy: CoherenceSync component is destroyed, GameObject remains and can no longer be synced over the network.\n\n" +
                                 "Disable: CoherenceSync component is disabled. You can reuse the instance for a different network entity by re-enabling it locally.")]
        private OnDestroyBehaviour onDestroyBehaviour;

        public void OnUniqueObjectReplaced(ICoherenceSync instance)
        {
        }

        public ICoherenceSync Instantiate(SpawnInfo spawnInfo)
        {
            return Object.Instantiate(spawnInfo.prefab as MonoBehaviour, spawnInfo.position, spawnInfo.rotation ?? Quaternion.identity) as ICoherenceSync;
        }

        public void WarmUpInstantiator(CoherenceBridge bridge, CoherenceSyncConfig config, INetworkObjectProvider assetLoader)
        {
        }

        public void Destroy(ICoherenceSync obj)
        {
            var monoBehaviour = obj as MonoBehaviour;

            if (onDestroyBehaviour == OnDestroyBehaviour.Destroy)
            {
                Object.Destroy(monoBehaviour);
            }
            else
            {
                monoBehaviour.enabled = false;
            }
        }

        public void OnApplicationQuit()
        {
        }
    }
}
