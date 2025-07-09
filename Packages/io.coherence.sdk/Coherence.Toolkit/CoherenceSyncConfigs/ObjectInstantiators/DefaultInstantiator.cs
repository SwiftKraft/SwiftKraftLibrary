// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    [DisplayName("Default", "Instances of this prefab will be instantiated and destroyed when they are no longer needed.")]
    public class DefaultInstantiator : INetworkObjectInstantiator
    {
        public void OnUniqueObjectReplaced(ICoherenceSync instance)
        {
        }

        public void WarmUpInstantiator(CoherenceBridge bridge, CoherenceSyncConfig config, INetworkObjectProvider assetLoader)
        {
        }

        public ICoherenceSync Instantiate(SpawnInfo spawnInfo)
        {
            return Object.Instantiate(spawnInfo.prefab as MonoBehaviour, spawnInfo.position, spawnInfo.rotation ?? Quaternion.identity) as ICoherenceSync;
        }

        public void Destroy(ICoherenceSync obj)
        {
            var monoBehaviour = obj as MonoBehaviour;

            if (obj.IsUnique && !string.IsNullOrEmpty(obj.ManualUniqueId))
            {
                monoBehaviour.gameObject.SetActive(false);
                return;
            }

            Object.Destroy(monoBehaviour.gameObject);
        }

        public void OnApplicationQuit()
        {
        }
    }
}
