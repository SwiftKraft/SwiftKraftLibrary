// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Archetypes
{
    using Log;
    using System;
    using System.Collections.Generic;
    using Bindings;
    using UnityEngine;

    [Serializable]
    internal class ToolkitArchetype
    {
        public CoherenceSync CoherenceSync { get; private set; }
        public List<Component> CachedComponents => cachedComponents;

        [Obsolete("Use CoherenceSync.ArchetypeName")]
        [Deprecated("4/3/2023", 1, 2, 0)]
        public string ArchetypeName => throw new NotImplementedException();
        internal List<ArchetypeLODStep> LODLevels => lodLevels;
        public List<ArchetypeComponent> BoundComponents => boundComponents;
        public Action<int> OnLODChanged;

        [SerializeField] private List<ArchetypeLODStep> lodLevels = new();

        private List<Component> cachedComponents = new();
        [SerializeField] private List<ArchetypeComponent> boundComponents = new();
        private readonly Dictionary<Component, ArchetypeComponent> indexedBoundComponents = new(new ByReferenceEqualityComparer());
        private int lastObservedLodLevel = 0;

        private static Coherence.Log.Logger logger = Log.GetLogger<ToolkitArchetype>();

        internal void Setup(CoherenceSync coherenceSync)
        {
            CoherenceSync = coherenceSync;
            Validate();
        }

        internal bool Validate()
        {
            // TODO move this to postprocessor / editor
            bool changed = false;

            if (lodLevels.Count == 0)
            {
                AddLODLevel();
                changed = true;
            }

            lodLevels[0].SetDistance(0);

            return changed;
        }

        /// <summary>
        /// A game object with just a single LOD level should not emit an
        /// archetype definition in the schema.
        ///
        /// Since the built-in components WorldPosition/Orientation/Scale
        /// can't be overriden uniquely on the original component,
        /// having field overrides on those will force LODing
        /// on the entity, even with a single Base step (i.e. LOD 0)
        /// </summary>
        [SerializeField] internal bool GeneratesArchetypeDefinition;

        /// <summary>
        /// This can (currently) only be called in editor mode,
        /// due to component.Bindings being null in runtime.
        /// Its result is stored in 'GeneratesArchetypeDefinition', which is serialized.
        /// </summary>
        internal bool RefreshGeneratesArchetypeDefinitionFlag()
        {
            var oldValue = GeneratesArchetypeDefinition;

            foreach (var component in boundComponents)
            {
                foreach (var binding in component.Bindings)
                {
                    if (binding.EnforcesLODingWhenFieldsOverriden)
                    {
                        var baseLODStep = binding.BindingArchetypeData.GetLODstep(0);

                        if (baseLODStep.IsOverriding)
                        {
                            GeneratesArchetypeDefinition = true;
                            return GeneratesArchetypeDefinition != oldValue;
                        }
                    }
                }
            }

            GeneratesArchetypeDefinition = lodLevels.Count > 1;
            return GeneratesArchetypeDefinition != oldValue;
        }

        public void AddLODLevel(bool fromEditor = false)
        {
            var newLOD = new ArchetypeLODStep();
            if (lodLevels.Count > 0)
            {
                var newDistance = lodLevels[lodLevels.Count - 1].Distance * 2f;
                if (newDistance == Mathf.Infinity || newDistance == 0)
                {
                    newDistance = 10;
                }
                newLOD.SetDistance(Mathf.Max(0, newDistance));
            }
            else
            {
                newLOD.SetDistance(0);
            }

            lodLevels.Add(newLOD);

            foreach (var boundComponent in boundComponents)
            {
                boundComponent.AddLODStep(lodLevels.Count - 1, fromEditor);
            }
        }

        public void SetLodLevelDistance(float newDistance, int lodStep)
        {
            var lodLevel = lodLevels[lodStep];
            var previousLodLevel = lodStep != 0 ? lodLevels[lodStep - 1] : null;
            var nextLodLevel = lodStep < lodLevels.Count - 1 ? lodLevels[lodStep + 1] : null;

            if (previousLodLevel != null && newDistance < previousLodLevel.Distance)
            {
                previousLodLevel.SetDistance(newDistance);
            }
            if (nextLodLevel != null && newDistance > nextLodLevel.Distance)
            {
                nextLodLevel.SetDistance(newDistance);
            }
            lodLevel.SetDistance(newDistance);
        }

        public void RemoveLodLevel(int index)
        {
            if (lodLevels.Count > index)
            {
                lodLevels.RemoveAt(index);
            }
            foreach (var syncedVariableHolder in boundComponents)
            {
                syncedVariableHolder.RemoveLODStep(index);
            }
        }

        #region UpdatingBoundVariables

        // Check each boundcomponent
        internal bool UpdateBoundVariables(CoherenceSync coherenceSync)
        {
            CoherenceSync = coherenceSync;

            bool changed = false;

            changed |= Validate();
            changed |= UpdateBindableComponents();

            // Get all components anc check if there are bindings for them on coherencesync
            foreach (var component in cachedComponents)
            {
                if (!component)
                {
                    continue;
                }

                foreach (var binding in new List<Binding>(CoherenceSync.Bindings))
                {
                    if (binding == null)
                    {
                        continue;
                    }

                    if (IsBindValid(binding, component))
                    {
                        var boundComponent = GetBoundComponentByComponent(component);
                        changed |= UpdateBinding(binding, boundComponent);
                    }
                }
            }

            // Any fields that are not currently bound are removed, should later be listed as missing
            var missingComponents = new List<ArchetypeComponent>();
            foreach (var boundComponent in boundComponents)
            {
                if (boundComponent.Component == null)
                {
                    missingComponents.Add(boundComponent);
                }
            }

            foreach (var boundComponent in missingComponents)
            {
                changed = true;
                boundComponents.Remove(boundComponent);
            }

            changed |= RefreshGeneratesArchetypeDefinitionFlag();

            return changed;
        }

        internal bool UpdateBindableComponents()
        {
            bool changed = false;

            CoherenceSync.GetComponentsInChildren(true, cachedComponents);

            IndexBoundComponents();

            foreach (var component in cachedComponents)
            {
                if (!component
                    || TypeUtils.IsNonBindableType(component.GetType())
                    || indexedBoundComponents.ContainsKey(component))
                {
                    continue;
                }

                changed = true;
                var boundComponent = new ArchetypeComponent(component, lodLevels.Count);
                indexedBoundComponents.Add(component, boundComponent);
                if (boundComponents.TrueForAll(x => !ReferenceEquals(x, boundComponent)))
                {
                    boundComponents.Add(boundComponent);
                }
            }

            foreach (var boundComponent in boundComponents)
            {
                boundComponent.ClearBindings(CoherenceSync);
                boundComponent.UpdateLODCountToArchetype(lodLevels.Count - 1);
            }

            return changed;
        }

        private void IndexBoundComponents()
        {
            indexedBoundComponents.Clear();

            foreach (var boundComponent in boundComponents)
            {
                if (boundComponent.Component && !indexedBoundComponents.TryAdd(boundComponent.Component, boundComponent))
                {
                    var component = boundComponent.Component;
                    var gameObjectNameAndLocation = $"'{component.name}'";
                    #if UNITY_EDITOR
                    gameObjectNameAndLocation += " in '" + UnityEditor.AssetDatabase.GetAssetOrScenePath(component) + "'";
                    #endif

                    using var loggerWithContext = Log.GetLogger<ToolkitArchetype>(component);

                    // If the duplicate entries are identical, it's safe to remove one of them automatically.
                    if (indexedBoundComponents.TryGetValue(boundComponent.Component, out var duplicate)
                        && (duplicate is null || duplicate.Equals(boundComponent)))
                    {
                        logger.Warning(Warning.ToolkitArchetypeComponentAlreadyBound,
                            $"{nameof(CoherenceSync)}.{nameof(CoherenceSync.Archetype)}.{nameof(BoundComponents)} property on GameObject {gameObjectNameAndLocation} " +
                            $"contains more than one entry for the Component {component.GetType().Name}. Removing the duplicate entry automatically.");

                        boundComponents.Remove(duplicate);

                        #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(CoherenceSync);
                        #endif

                        // Restart the process from the beginning, since we've modified the boundComponents list mid-iteration.
                        IndexBoundComponents();
                        return;
                    }

                    logger.Warning(Warning.ToolkitArchetypeComponentAlreadyBound,
                        $"{nameof(CoherenceSync)}.{nameof(CoherenceSync.Archetype)}.{nameof(BoundComponents)} property on GameObject {gameObjectNameAndLocation} " +
                        $"contains more than one entry for the Component {component.GetType().Name}. Could not merge the duplicate entries automatically, because their data is not identical. You can fix the problem by inspecting the CoherenceSync using the Inspector with Debug Mode enabled and deleting duplicate entries manually.");
                }
            }
        }

        private bool IsBindValid(Binding binding, Component component)
        {
            if (binding.Descriptor == null || string.IsNullOrEmpty(binding.Descriptor.MonoAssemblyType))
            {
                return false;
            }

            if (!IsComponentSameAndBindable(component, binding.unityComponent))
            {
                return false;
            }

            return binding.IsValid;
        }

        private bool IsComponentSameAndBindable(Component component, Component bindingComponent)
        {
            if (component && TypeUtils.IsNonBindableType(component.GetType()))
            {
                return false;
            }

            if (bindingComponent != component)
            {
                return false;
            }

            return true;
        }

        private bool UpdateBinding(Binding binding, ArchetypeComponent boundComponent)
        {
            bool changed = false;

            SchemaType schemaType = TypeUtils.GetSchemaType(binding.MonoAssemblyRuntimeType);
            changed |= binding.CreateArchetypeData(schemaType, boundComponent.MaxLods);
            changed |= boundComponent.AddBinding(binding, schemaType);

            return changed;
        }

        internal void SetBindingActive(Binding binding, ArchetypeComponent boundComponent, bool active)
        {
            if (active)
            {
                if (!CoherenceSync.Bindings.Contains(binding))
                {
                    CoherenceSyncBindingHelper.AddBinding(CoherenceSync, binding.UnityComponent, binding.Descriptor);
                    UpdateBinding(binding, boundComponent);
                }
            }
            else
            {
                if (CoherenceSync.Bindings.Contains(binding))
                {
                    CoherenceSyncBindingHelper.RemoveBinding(CoherenceSync, binding.UnityComponent, binding.Descriptor);
                    boundComponent.RemoveBinding(binding);
                }
            }
        }

        #endregion

        public int GetTotalActiveBitsOfLOD(int lodStep)
        {
            int bits = 0;
            foreach (ArchetypeComponent boundComponent in boundComponents)
            {
                if (boundComponent.HasSyncedBindings() && boundComponent.LodStepsActive > lodStep)
                {
                    bits += boundComponent.GetTotalBitsOfLOD(lodStep);
                }
            }
            return bits;
        }

        public int GetLargestLOD()
        {
            int bits = 0;
            for (int i = 0; i < lodLevels.Count; i++)
            {
                bits = Mathf.Max(bits, GetTotalActiveBitsOfLOD(i));
            }
            return bits;
        }

        public ArchetypeComponent GetBoundComponentByComponent(Component component)
        {
            indexedBoundComponents.TryGetValue(component, out var boundComponent);
            return boundComponent;
        }

        internal int GetTotalBindings(bool methods)
        {
            int totalBindings = 0;
            foreach (ArchetypeComponent component in boundComponents)
            {
                int bindingCount = methods ? component.GetTotalActiveMethodBindings() : component.GetTotalActiveValueBindings();
                totalBindings += bindingCount;
            }
            return totalBindings;
        }

        // Used by other assemblies

        public void SetObservedLodLevel(int newObservedLodLevel)
        {
            if (newObservedLodLevel != lastObservedLodLevel &&
               OnLODChanged != null)
            {
                OnLODChanged(newObservedLodLevel);
            }

            lastObservedLodLevel = newObservedLodLevel;
        }

        public int LastObservedLodLevel => lastObservedLodLevel;

        private sealed class ByReferenceEqualityComparer : IEqualityComparer<Component>
        {
            public bool Equals(Component x, Component y) => ReferenceEquals(x, y);
            public int GetHashCode(Component obj) => obj.GetHashCode();
        }
    }
}
