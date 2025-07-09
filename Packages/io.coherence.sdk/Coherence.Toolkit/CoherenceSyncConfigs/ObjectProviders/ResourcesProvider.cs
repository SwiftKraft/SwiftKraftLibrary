// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable, DisplayName("Resources", "Load this prefab using the Resources system. The prefab must be inside a Resources folder.")]
    public sealed class ResourcesProvider : INetworkObjectProvider
    {
        [SerializeField, ReadOnly(false)]
        private string resourcesPath;

        private ICoherenceSync syncObject;
        private int references = 0;

        public int References => references;

        public void LoadAsset(string networkAssetId, Action<ICoherenceSync> onLoaded)
        {
            GetSyncObject(networkAssetId);

            onLoaded.Invoke(syncObject);
        }

        public ICoherenceSync LoadAsset(string networkAssetId)
        {
            GetSyncObject(networkAssetId);

            return syncObject;
        }

        private void GetSyncObject(string networkAssetId)
        {
            references++;
            syncObject = (syncObject != null) ? syncObject : LoadCoherenceSync();

            if (syncObject == null)
            {
                Debug.LogError($"Asset with ID {networkAssetId} failed to load from Resources with path {resourcesPath}. " +
                               $"Please double check the Asset configuration.");
            }
        }

        public void Release(ICoherenceSync obj)
        {
            references--;

            if (references == 0)
            {
                syncObject = null;
            }
        }

        public void OnApplicationQuit()
        {
            references = 0;
            syncObject = null;
        }

        private ICoherenceSync LoadCoherenceSync()
        {
            var obj = Resources.Load<Object>(resourcesPath);

            if (obj is GameObject go)
            {
                return go.GetComponent<ICoherenceSync>();
            }

            return obj as ICoherenceSync;
        }

        public void Initialize(CoherenceSyncConfig entry)
        {
#if UNITY_EDITOR
           resourcesPath = GetResourcesPath(entry);
           UnityEditor.EditorUtility.SetDirty(entry);
#endif
        }

        public bool Validate(CoherenceSyncConfig entry)
        {
#if UNITY_EDITOR
            var newResourcesPath = GetResourcesPath(entry);

            return resourcesPath.Equals(newResourcesPath);
#else
            return true;
#endif
        }

#if UNITY_EDITOR
        private string GetResourcesPath(CoherenceSyncConfig entry)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(entry.EditorTarget);
            var index = path.IndexOf("/Resources/");

            if (index == -1)
            {
                return string.Empty;
            }

            path = path.Remove(0, index);
            path = path.Replace("/Resources/", string.Empty);
            return path.Remove(path.IndexOf("."));
        }
#endif
    }
}

