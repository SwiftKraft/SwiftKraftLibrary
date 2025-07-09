// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;

    [Serializable, DisplayName("Direct Reference", "Load this prefab using a direct reference to the asset. The prefab cannot be inside a Resources folder.")]
    public sealed class DirectReferenceProvider : INetworkObjectProvider
    {
        [SerializeField, ReadOnly(false)]
        private GameObject prefab;

        public void LoadAsset(string networkAssetId, Action<ICoherenceSync> onLoaded)
        {
            onLoaded.Invoke(prefab.GetComponent<ICoherenceSync>());
        }

        public ICoherenceSync LoadAsset(string networkAssetId)
        {
            return prefab.GetComponent<ICoherenceSync>();
        }

        public void Release(ICoherenceSync obj)
        {
        }

        public void OnApplicationQuit()
        {
        }

        public void Initialize(CoherenceSyncConfig entry)
        {
#if UNITY_EDITOR
            prefab = entry.EditorTarget as GameObject;
            UnityEditor.EditorUtility.SetDirty(entry);
#endif
        }

        public bool Validate(CoherenceSyncConfig entry)
        {
#if UNITY_EDITOR
            var prefabInEntry = entry.EditorTarget as GameObject;

            return prefabInEntry == prefab;
#else
            return true;
#endif
        }
    }
}

