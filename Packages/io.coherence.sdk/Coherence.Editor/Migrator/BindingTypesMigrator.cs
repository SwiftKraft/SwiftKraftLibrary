// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Common;
    using UnityEditor;
    using UnityEngine;

    [Preserve]
    internal class BindingTypesMigrator : IDataMigrator
    {
        public SemVersion MaxSupportedVersion => new(3);
        public int Order => 1;
        public string MigrationMessage => "Updated Prefab Bindings to accomodate new Binding types.";

        public void Initialize()
        {
        }

        public IEnumerable<Object> GetMigrationTargets()
        {
            var prefabs = new List<CoherenceSync>();
            CoherenceSyncConfigRegistry.Instance.GetReferencedPrefabs(prefabs);
            return prefabs;
        }

        public bool RequiresMigration(Object obj)
        {
            return obj is CoherenceSync;
        }

        public bool MigrateObject(Object obj)
        {
            if (obj is not CoherenceSync sync)
            {
                return false;
            }

            var anyChanged = false;
            for (var i = 0; i < sync.Bindings.Count; i++)
            {
                var binding = sync.Bindings[i];

                if (binding == null)
                {
                    continue;
                }
#pragma warning disable CS0618
                if (binding.isPredicted)
                {
                    var hasInput = sync.TryGetComponent<CoherenceInput>(out var _);
                    anyChanged = true;
                    binding.isPredicted = false;
                    binding.predictionMode = hasInput ? PredictionMode.InputAuthority : PredictionMode.Never;

                    if (!hasInput)
                    {
                        Debug.LogWarning($"Binding {binding.Name} of prefab {sync.name} was using client prediction but the prefab wasn't using CoherenceInput component. Client prediction has been disabled for the affected binding.");
                    }
                }
#pragma warning restore CS0618

                anyChanged |= MigrateType(binding, sync, i);
            }

            if (anyChanged)
            {
                EditorUtility.SetDirty(sync);
            }

            return anyChanged;
        }

        private static bool MigrateType(Binding binding, CoherenceSync sync, int i)
        {
            var descriptors = EditorCache.GetComponentDescriptors(binding.UnityComponent);
            var newDescriptor = descriptors.FirstOrDefault(d => binding.Descriptor == d);

            if (newDescriptor == null || newDescriptor.BindingType == binding.GetType())
            {
                return false;
            }

            var newBinding = newDescriptor.InstantiateBinding(binding.UnityComponent);

            var schemaType = TypeUtils.GetSchemaType(binding.MonoAssemblyRuntimeType);
            newBinding.CreateArchetypeData(schemaType, 1);
            newBinding.archetypeData.SetSampleRate(binding.archetypeData.SampleRate);

            newBinding.interpolationSettings = binding.interpolationSettings;
            newBinding.routing = binding.routing;
            newBinding.guid = binding.guid;
#pragma warning disable 612,618
            newBinding.isPredicted = binding.isPredicted;
#pragma warning restore 612,618

            sync.Bindings[i] = newBinding;

            return true;
        }
    }
}
