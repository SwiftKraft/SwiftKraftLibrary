// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
#if HAS_ADDRESSABLES
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class CoherenceSyncAddressableChangeEvents
    {
        static CoherenceSyncAddressableChangeEvents() =>
            AddressableAssetSettings.OnModificationGlobal += OnAddressableSettingsChanged;

        private static void OnAddressableSettingsChanged(AddressableAssetSettings assetSettings, AddressableAssetSettings.ModificationEvent modificationEvent, object eventData)
        {
            switch (modificationEvent)
            {
                case AddressableAssetSettings.ModificationEvent.EntryAdded:
                    EntryAdded(FilterEvents(eventData));
                    break;
                case AddressableAssetSettings.ModificationEvent.EntryRemoved:
                    EntryRemoved(FilterEvents(eventData));
                    break;
            }
        }

        private static void EntryRemoved(List<CoherenceSync> entries)
        {
            // Prefab HAS CS, HAS AddressableProvider and user makes it non-addressable
            // Provider should change from AddressableProvider to whatever fits the most (basically call
            // InitializeProvider again)
            foreach (var entry in entries.Where(entry => entry.CoherenceSyncConfig.Provider is AddressablesProvider))
            {
                CoherenceSyncConfigUtils.InitializeProvider(entry.CoherenceSyncConfig);
            }
        }

        private static void EntryAdded(List<CoherenceSync> entries)
        {
            // Prefab HAS CS, has a default provider and user makes it addressable
            // Provider should change from default provider to AddressableProvider
            foreach (var entry in entries.Where(entry =>
                         CoherenceSyncConfigUtils.ProviderIsBuiltIn(entry.CoherenceSyncConfig.Provider)))
            {
                CoherenceSyncConfigUtils.InitializeProvider(entry.CoherenceSyncConfig);
            }
        }

        private static List<CoherenceSync> FilterEvents(object eventData) =>
            (eventData as List<AddressableAssetEntry> ?? new List<AddressableAssetEntry>())
            .Where(entry => entry.TargetAsset is GameObject go && go.GetComponent<CoherenceSync>() != null)
            .Select(entry => (entry.TargetAsset as GameObject)?.GetComponent<CoherenceSync>())
            .ToList();
    }
#endif
}
