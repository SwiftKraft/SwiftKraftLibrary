// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
#if HAS_ADDRESSABLES
    using UnityEditor.AddressableAssets;
#endif
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Utility class for <see cref="CoherenceSyncConfig"/> related operations.
    /// </summary>
    public static class CoherenceSyncConfigUtils
    {
        /// <summary>
        /// Raised after the asset is created and initialized through <see cref="Create"/>,
        /// but before registration on the <see cref="CoherenceSyncConfigRegistry"/>.
        /// </summary>
        public static event Action<CoherenceSyncConfig> OnAfterConfigCreated;

        /// <summary>
        /// Raised before a <see cref="CoherenceSyncConfig"/> asset is deleted through <see cref="Delete"/>.
        /// </summary>
        public static event Action<CoherenceSyncConfig> OnBeforeConfigDeleted;

        /// <summary>
        /// Raised after <see cref="CoherenceSyncConfig.Provider"/> changes through a <see cref="CreateObjectProvider"/> call.
        /// </summary>
        public static event Action<CoherenceSyncConfig, INetworkObjectProvider, INetworkObjectProvider> OnObjectProviderChanged;

        /// <summary>
        /// Raised after <see cref="CoherenceSyncConfig.Instantiator"/> changes through a <see cref="CreateObjectInstantiator"/> call.
        /// </summary>
        public static event Action<CoherenceSyncConfig, INetworkObjectInstantiator, INetworkObjectInstantiator> OnObjectInstantiatorChanged;

        public static bool TryGetFromAsset(Object obj, out CoherenceSyncConfig config)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _))
            {
                config = default;
                return false;
            }

            return CoherenceSyncConfigRegistry.Instance.TryGetFromAssetId(guid, out config);
        }

        /// <summary>
        /// Delete an existing <see cref="CoherenceSyncConfig"/>.
        /// If it's an asset, the asset will be deleted. This includes it being a subasset.
        /// If it's not an asset, it's destroyed.
        ///
        /// Modifying several subassets on the same asset while on a batch (AssetDatabase.[Start|Stop]AssetEditing)
        /// will corrupt the asset.
        /// </summary>
        public static bool Delete(CoherenceSyncConfig config)
        {
            try
            {
                OnBeforeConfigDeleted?.Invoke(config);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            var path = AssetDatabase.GetAssetPath(config);
            if (AssetDatabase.IsMainAsset(config))
            {
                return AssetDatabase.DeleteAsset(path);
            }

            var mainAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (mainAsset)
            {
                AssetDatabase.RemoveObjectFromAsset(config);
                Object.DestroyImmediate(config, true);
                EditorUtility.SetDirty(mainAsset);
                AssetDatabase.SaveAssetIfDirty(mainAsset);
                AssetDatabase.ImportAsset(path);
            }
            else
            {
                // There's a chance the object is in memory, referenced by other objects, yet there's no serialized
                // representation of it on disk. In such case, let's make sure it gets destroyed.
                Object.DestroyImmediate(config);
            }
            return true;
        }

        /// <summary>
        ///     Create an CoherenceSync Config for the given Unity Object.
        /// </summary>
        /// <remarks>
        ///     Only Prefabs with the CoherenceSync component attached are supported as Networked Prefabs at the moment.
        /// </remarks>
        /// <param name="obj">Reference to the Unity Object that you want to create a CoherenceSync Config for.</param>
        /// <returns>
        ///     The created CoherenceSyncConfig, or the existing one.
        /// </returns>
        public static CoherenceSyncConfig Create(Object obj)
        {
            if (TryGetFromAsset(obj, out var config))
            {
                return config;
            }

            if (!IsNetworkSupportedForObject(obj).isSupported)
            {
                return null;
            }

            config = ScriptableObject.CreateInstance<CoherenceSyncConfig>();
            config.Init(obj);
            InitializeProvider(config);
            CreateConfigAsset(config);
            config.UpdateSelfId();

            try
            {
                OnAfterConfigCreated?.Invoke(config);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            var registry = CoherenceSyncConfigRegistry.Instance;
            registry.Register(config);

            return config;
        }

        internal static void CreateConfigAsset(CoherenceSyncConfig config)
        {
            Directory.CreateDirectory(Paths.coherenceSyncConfigPath);
            var path = Path.Combine(Paths.coherenceSyncConfigPath, config.name + ".asset");
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(config, uniquePath);
        }

        /// <summary>
        /// Checks if an unlinked config can be linked.
        /// </summary>
        /// <param name="config"></param>
        /// <returns><see langword="true"/> if linkage is possible; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="config"/> is <see langword="null"/>.</exception>
        public static bool CanLink(CoherenceSyncConfig config)
        {
            if (!config)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.IsLinked)
            {
                return false;
            }

            if (string.IsNullOrEmpty(config.ID))
            {
                return false;
            }

            var path = AssetDatabase.GUIDToAssetPath(config.ID);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (CoherenceSyncConfigRegistry.Instance.TryGetFromAssetId(config.ID, out var registeredConfig))
            {
                if (registeredConfig != config)
                {
                    return false;
                }
            }

            var sync = AssetDatabase.LoadAssetAtPath<CoherenceSync>(path);
            return sync && sync.CoherenceSyncConfig;
        }

        /// <summary>
        /// Attempt to link a <see cref="CoherenceSyncConfig"/> with its <see cref="CoherenceSync"/> counterpart.
        /// </summary>
        /// <remarks>
        /// Linking tries to locate a <see cref="CoherenceSync"/> asset based on the
        /// <see cref="CoherenceSyncConfig.ID"/>. It also sets the <see cref="CoherenceSync.CoherenceSyncConfig"/>
        /// property on the <see cref="CoherenceSync"/> component.
        /// </remarks>
        /// <param name="config">The <see cref="CoherenceSyncConfig"/> to link.</param>
        /// <returns><see langword="true"/> if linkage was successful; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="config"/> is <see langword="null"/>.</exception>
        /// <seealso cref="CanLink"/>
        public static bool Link(CoherenceSyncConfig config)
        {
            if (!config)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.IsLinked)
            {
                return false;
            }

            if (string.IsNullOrEmpty(config.ID))
            {
                return false;
            }

            var path = AssetDatabase.GUIDToAssetPath(config.ID);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var sync = AssetDatabase.LoadAssetAtPath<CoherenceSync>(path);
            if (!sync)
            {
                return false;
            }

            config.Init(sync.gameObject);
            sync.CoherenceSyncConfig = config;
            EditorUtility.SetDirty(sync);
            return true;
        }

        private static bool IsInResources(string path)
        {
#if NET_UNITY_4_8
            return path.Replace('\\', '/').Contains("/Resources/", StringComparison.OrdinalIgnoreCase);
#else
            return path.Replace('\\', '/').ToLower().Contains("/resources/");
#endif
        }


        internal static bool IsAddressable(CoherenceSyncConfig config)
        {
#if HAS_ADDRESSABLES
            var path = AssetDatabase.GetAssetPath(config.EditorTarget);
            var guid = AssetDatabase.AssetPathToGUID(path);
            return AddressableAssetSettingsDefaultObject.Settings is { } settings &&
                   settings.FindAssetEntry(guid) != null;
#else
            return false;
#endif
        }


        internal static void InitializeProvider(CoherenceSyncConfig config)
        {
            if (config.Provider != null)
            {
                if (!ProviderIsBuiltIn(config.Provider))
                {
                    return;
                }
            }

            var path = AssetDatabase.GetAssetPath(config.EditorTarget);
            var guid = AssetDatabase.AssetPathToGUID(path);

            if (IsInResources(path))
            {
                config.Provider = new ResourcesProvider();
            }
#if HAS_ADDRESSABLES
            else if (AddressableAssetSettingsDefaultObject.Settings is { } settings && settings.FindAssetEntry(guid) != null)
            {
                config.Provider = new AddressablesProvider();
            }
#endif
            else
            {
                config.Provider = new DirectReferenceProvider();
            }

            config.Provider.Initialize(config);

            EditorUtility.SetDirty(config);
        }

        private static (bool isSupported, bool isPrefabInstance) IsNetworkSupportedForObject(Object obj)
        {
            if (obj == null)
            {
                return (false, false);
            }

            var prefabType = PrefabUtility.GetPrefabAssetType(obj);
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);

            return ((prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
                    && !string.IsNullOrEmpty(guid) && instanceStatus == PrefabInstanceStatus.NotAPrefab,
                instanceStatus == PrefabInstanceStatus.Connected);
        }

        /// <summary>
        /// Creates a new instance of a type implementing <see cref="INetworkObjectProvider"/>
        /// and assigns it to the given <see cref="CoherenceSyncConfig"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the provider can be changed; <see langword="false"/> otherwise.
        /// </returns>
        public static bool CreateObjectProvider(CoherenceSyncConfig config, Type type)
        {
            if (!ValidateProviderTypeChange(config, type))
            {
                return false;
            }

            var newProvider = (INetworkObjectProvider)Activator.CreateInstance(type);
            newProvider?.Initialize(config);

            var oldProvider = config.Provider;
            config.Provider = newProvider;
            EditorUtility.SetDirty(config);

            try
            {
                OnObjectProviderChanged?.Invoke(config, oldProvider, newProvider);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return true;
        }

        /// <summary>
        /// Creates a new instance of a type implementing <see cref="INetworkObjectInstantiator"/>
        /// and assigns it to the given <see cref="CoherenceSyncConfig"/>.
        /// </summary>
        public static void CreateObjectInstantiator(CoherenceSyncConfig config, Type type)
        {
            var newInstantiator = (INetworkObjectInstantiator)Activator.CreateInstance(type);

            var oldInstantiator = config.Instantiator;
            config.Instantiator = newInstantiator;
            EditorUtility.SetDirty(config);

            try
            {
                OnObjectInstantiatorChanged?.Invoke(config, oldInstantiator, newInstantiator);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static bool ValidateProviderTypeChange(CoherenceSyncConfig config, Type newProvider)
        {
            if (!AssetMover.MoveAssetPathIfNeeded(newProvider, config.EditorTarget))
            {
                return false;
            }

#if HAS_ADDRESSABLES
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(config.EditorTarget, out var guid, out long _))
            {
                return false;
            }

            if (config.Provider is not AddressablesProvider && newProvider == typeof(AddressablesProvider))
            {
                var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (!addressableSettings)
                {
                    Debug.LogWarning("Could not find Addressables default object's settings.");
                    return false;
                }

                var addressableEntry = addressableSettings.FindAssetEntry(guid);
                if (addressableEntry == null)
                {
                    addressableSettings.CreateOrMoveEntry(guid, addressableSettings.DefaultGroup);
                }
            }
#endif

            return true;
        }

        internal static EntryInfo GetNetworkedObjectsInfo(out Dictionary<CoherenceSyncConfig, EntryInfo> entryInfos)
        {
            entryInfos = new Dictionary<CoherenceSyncConfig, EntryInfo>();

            var totalInfo = new EntryInfo();

            foreach (var config in CoherenceSyncConfigRegistry.Instance)
            {
                if (!config || config.EditorTarget is not GameObject go)
                {
                    totalInfo.MissingAssets++;
                    continue;
                }

                if (!go.TryGetComponent(out CoherenceSync sync))
                {
                    totalInfo.MissingAssets++;
                    continue;
                }

                var entryInfo = GetBindingInfo(sync).Value;

                totalInfo.Variables += entryInfo.Variables;
                totalInfo.Methods += entryInfo.Methods;
                totalInfo.Bits += entryInfo.Bits;
                totalInfo.ComponentActions += entryInfo.ComponentActions;
                totalInfo.InvalidBindings += entryInfo.InvalidBindings;
                totalInfo.NetworkComponents += entryInfo.NetworkComponents;

                if (entryInfo.InvalidBindings > 0)
                {
                    totalInfo.AssetsWithInvalidBindings++;
                }

                entryInfos.Add(config, entryInfo);
            }

            return totalInfo;
        }

        internal static EntryInfo? GetBindingInfo(CoherenceSync sync)
        {
            var (variables, invalidVariables) = CoherenceSyncUtils.GetVariableBindingsCount(sync, sync.gameObject);
            var (methods, invalidMethods) = CoherenceSyncUtils.GetMethodBindingsCount(sync, sync.gameObject);
            var bits = 0;
            var invalid = invalidVariables + invalidMethods;
            // Initialize to 1 because every network entity has an Asset Id component
            var networkComponents = 1;
            var uniqueUnityComponentsBound = new HashSet<Component>();
            var componentActions = sync.componentActions?.Length ?? 0;
            var bindingsWithInputAuthorityPrediction = 0;

            if (sync.Bindings == null)
            {
                return null;
            }

            foreach (var binding in sync.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (uniqueUnityComponentsBound.Add(binding.UnityComponent))
                {
                    networkComponents++;
                }

                if (!binding.IsMethod)
                {
                    var schemaType = TypeUtils.GetSchemaType(binding.MonoAssemblyRuntimeType);
                    bits += BindingLODStepData.GetDefaultOverrides(schemaType).bits *
                            ArchetypeMath.GetBitsMultiplier(schemaType);
                }

                if (binding.predictionMode == PredictionMode.InputAuthority)
                {
                    bindingsWithInputAuthorityPrediction++;
                }
            }

            if (sync.lifetimeType == CoherenceSync.LifetimeType.Persistent)
            {
                networkComponents++;
            }

            if (sync.uniquenessType == CoherenceSync.UniquenessType.NoDuplicates)
            {
                networkComponents++;
            }

            if (sync.preserveChildren)
            {
                networkComponents++;
            }

            return new EntryInfo
            {
                Variables = variables,
                Methods = methods,
                Bits = bits,
                InvalidBindings = invalid,
                ComponentActions = componentActions,
                NetworkComponents = networkComponents,
                BindingsWithInputAuthPrediction = bindingsWithInputAuthorityPrediction,
            };
        }

        internal static bool ProviderIsBuiltIn(INetworkObjectProvider provider)
        {
            return provider is ResourcesProvider or
#if HAS_ADDRESSABLES
                AddressablesProvider or
#endif
                DirectReferenceProvider;
        }

        internal static bool ProviderIsAddressableOrCustom(INetworkObjectProvider provider) =>
#if HAS_ADDRESSABLES
            (provider is AddressablesProvider) ||
#endif
            !ProviderIsBuiltIn(provider);

        internal static void ExtractSubAssets(IEnumerable<Object> targets)
        {
            foreach (var target in targets)
            {
                if (target is not CoherenceSyncConfig config)
                {
                    continue;
                }

                if (!AssetDatabase.IsSubAsset(config))
                {
                    continue;
                }

                ExtractSubAsset(config);
            }
        }

        internal static void ExtractSubAssets(CoherenceSyncConfigRegistry registry)
        {
            for (var i = 0; i < registry.Count; i++)
            {
                var config = registry.GetAt(i);
                if (!AssetDatabase.IsSubAsset(config))
                {
                    continue;
                }

                ExtractSubAsset(config);
                i--;
            }
        }

        internal static void ExtractSubAsset(CoherenceSyncConfig config)
        {
            AssetDatabase.RemoveObjectFromAsset(config);
            CreateConfigAsset(config);

            // We're assuming all these configs come from the registry (as they should),
            // so that we don't have to lose CPU time resolving the main asset.
            // In the unlikely scenario where users create configs and add them as subobjects of any other main asset,
            // said asset won't be updated properly - users would have to do call the following operations on their own.

            // When it comes to working with SubAssets, each modification requires the main asset to be re-imported,
            // or else asset might get corrupted.

            var registry = CoherenceSyncConfigRegistry.Instance;
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssetIfDirty(registry);
            var assetPath = AssetDatabase.GetAssetPath(registry);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        internal static void DeleteUnlinked(IEnumerable<Object> objects)
        {
            // We can't batch-delete since batch-editing subassets corrupts them.
            // Configs might be subassets of the registry.
            foreach (var obj in objects)
            {
                if (obj is not CoherenceSyncConfig config)
                {
                    continue;
                }

                if (config.IsLinked)
                {
                    continue;
                }

                Delete(config);
            }
        }

        internal static void DeleteConfigsWithIssues()
        {
            var registry = CoherenceSyncConfigRegistry.Instance;

            for (var i = 0; i < registry.LeakedCount; i++)
            {
                var config = registry.GetLeakedAt(i);
                i--;
                Delete(config);
            }

            // We can't batch-delete since batch-editing subassets corrupts them.
            // Configs might be subassets of the registry.
            for (var i = 0; i < registry.Count; i++)
            {
                var config = registry.GetAt(i);
                if (config.IsLinked)
                {
                    continue;
                }

                if (Delete(config))
                {
                    i--;
                }
            }
        }

        internal static bool AnyIssues
        {
            get
            {
                var registry = CoherenceSyncConfigRegistry.Instance;
                if (registry.LeakedCount > 0)
                {
                    return true;
                }

                foreach (var config in registry)
                {
                    if (!config.IsLinked)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal static bool AnySubassets
        {
            get
            {
                var registry = CoherenceSyncConfigRegistry.Instance;
                foreach (var config in registry)
                {
                    if (AssetDatabase.IsSubAsset(config))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
