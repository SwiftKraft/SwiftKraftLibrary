// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ExcludeFromDropdown]
    public sealed class AssetDatabaseProvider : INetworkObjectProvider
    {
        public void LoadAsset(string networkAssetId, Action<ICoherenceSync> onLoaded)
        {
#if UNITY_EDITOR
            if (CoherenceSyncConfigRegistry.Instance.TryGetFromAssetId(networkAssetId, out var config))
            {
                if (config.EditorTarget is GameObject go &&
                    go.TryGetComponent(out ICoherenceSync sync))
                {
                    onLoaded?.Invoke(sync);
                }
            }
#endif
        }

        public ICoherenceSync LoadAsset(string networkAssetId)
        {
            return null;
        }

        public void Release(ICoherenceSync obj)
        {
        }

        public void OnApplicationQuit()
        {
        }

        public void Initialize(CoherenceSyncConfig entry)
        {
        }

        public bool Validate(CoherenceSyncConfig entry)
        {
            return true;
        }
    }
}
