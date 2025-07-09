// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
#if HAS_ADDRESSABLES
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    [Serializable, DisplayName("Addressables", "Load this Prefab using the Addressables package.")]
    public sealed class AddressablesProvider : INetworkObjectProvider
    {
        /// <summary>
        /// Number of references to this asset.
        /// </summary>
        public int References => references;

        /// <summary>
        /// Is the asset loaded and available to instantiate.
        /// </summary>
        public bool IsAssetLoaded => assetReference?.IsValid() ?? false;

        [SerializeField]
        private AssetReference assetReference;

        private int references;

        /// <summary>
        /// Load the asset async.
        /// </summary>
        /// <remarks>
        /// If the asset is alreay loaded the callback is called.
        /// Each reference to the asset increments the reference count.
        /// </remarks>
        /// <param name="networkAssetId">The assset id used to reference this asset.</param>
        /// <param name="onLoaded">Callback when the asset is loaded.</param>
        public void LoadAsset(string networkAssetId, Action<ICoherenceSync> onLoaded)
        {
            if (assetReference.IsValid())
            {
                if (assetReference.IsDone)
                {
                    InvokeCallback(assetReference.OperationHandle.Result as GameObject, onLoaded);
                }
                else
                {
                    // Handle is automatically released after the callback.
                    assetReference.OperationHandle.Completed += (handle) => {
                         InvokeCallback(handle.Result as GameObject, onLoaded);
                    };
                }
                return;
            }

            assetReference.LoadAssetAsync<GameObject>().Completed += newHandle =>
            {
                if (newHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    InvokeCallback(assetReference.OperationHandle.Result as GameObject, onLoaded);
                }
                else
                {
                    Debug.LogError($"Asset with ID {networkAssetId} failed to load from Addressables. " +
                                 "Please double check the Asset configuration.");
                }
            };
        }

        /// <summary>
        /// Load the asset sync.
        /// </summary>
        /// <remarks>
        /// If the asset is alreay loaded the existing reference is returned.
        /// Each reference to the asset increments the reference count.
        /// </remarks>
        /// <param name="networkAssetId">The assset id used to reference this asset.</param>
        /// <returns>ICoherenceSync reference from the loaded asset.</returns>
        public ICoherenceSync LoadAsset(string networkAssetId)
        {
            if (assetReference.IsValid())
            {
                references++;
                return (assetReference.OperationHandle.Result as GameObject).GetComponent<ICoherenceSync>();
            }

            var result = assetReference.LoadAssetAsync<GameObject>().WaitForCompletion();

            if (result != null)
            {
                references++;
                return (assetReference.OperationHandle.Result as GameObject).GetComponent<ICoherenceSync>();
            }

            Debug.LogError($"Asset with ID {networkAssetId} failed to load from Addressables. " +
                           "Please double check the Asset configuration.");

            return null;
        }

        /// <summary>
        /// Releases a reference to the loaded asset.
        /// </summary>
        /// <param name="obj">The ICoherenceSync reference this asset.</param>
        public void Release(ICoherenceSync obj)
        {
            references--;

            if (references == 0 && assetReference.IsValid())
            {
                assetReference.ReleaseAsset();
            }
        }

        /// <summary>
        /// Releases the loaded asset.  Called when the application is quit.
        /// </summary>
        public void OnApplicationQuit()
        {
            if (assetReference.IsValid())
            {
                assetReference.ReleaseAsset();
            }

            references = 0;
        }

        /// <summary>
        /// Initializes the asset reference but does not trigger a load of the asset.
        /// </summary>
        /// <param name="entry">The CoherenceSyncConfig asset definitino.</param>
        public void Initialize(CoherenceSyncConfig entry)
        {
            assetReference = new AssetReference(entry.ID);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(entry);
#endif
        }

        /// <summary>
        /// Initializes the asset reference but does not trigger a load of the asset.
        /// </summary>
        /// <param name="entry">The CoherenceSyncConfig asset definitino.</param>
        /// <returns>Bool True if the reference is correctly setup or if the Unity context is not the editor.</returns>
        public bool Validate(CoherenceSyncConfig entry)
        {
#if UNITY_EDITOR
            return assetReference != null && assetReference.editorAsset == entry.EditorTarget && assetReference.AssetGUID == entry.ID;
#else
            return true;
#endif
        }

        private void InvokeCallback(GameObject go, Action<ICoherenceSync> onLoaded)
        {
            references++;
            onLoaded.Invoke(go.GetComponent<ICoherenceSync>());
        }
    }
#endif
}
