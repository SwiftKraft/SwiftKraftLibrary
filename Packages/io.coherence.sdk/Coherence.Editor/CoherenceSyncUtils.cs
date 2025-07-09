namespace Coherence.Editor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Coherence.Toolkit.Bindings.TransformBindings;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using static Coherence.Toolkit.CoherenceSync;

    /// <summary>
    /// Utility class for <see cref="CoherenceSync"/> related operations.
    /// </summary>
    public static class CoherenceSyncUtils
    {
        private const string managedReferencePath = "managedReferences";

        /// <summary>
        /// Paths of serialized properties that are not exposed in the Inspector
        /// and should never be overridden in any prefab instances by design.
        /// </summary>
        internal static readonly string[] UndesiredOverrides =
        {
            Property.coherenceSyncConfig,
            Property.bakedScriptType,
            Property.bindings,
            Property.archetype,
            Property.componentActions,
            // detect modified bindings or componentActions (both have [SerializeReference])
            managedReferencePath
        };

        /// <summary>
        /// Raised before adding a binding to <see cref="CoherenceSync.Bindings"/>.
        /// </summary>
        public static event Action<CoherenceSync, Binding> OnBeforeBindingAdded;

        /// <summary>
        /// Raised before deleting a binding from <see cref="CoherenceSync.Bindings"/>.
        /// </summary>
        public static event Action<CoherenceSync, Binding> OnBeforeBindingRemoved;

        /// <inheritdoc cref="AddBinding(CoherenceSync,Component,string)" />
        public static Binding AddBinding<T>(GameObject gameObject, string memberName) where T : Component
        {
            var sync = GetCoherenceSync(gameObject);
            _ = gameObject.TryGetComponent(out T component);

            return AddBinding(sync, component, memberName);
        }

        /// <inheritdoc cref="AddBinding(CoherenceSync,Component,string)" />
        public static Binding AddBinding(GameObject gameObject, Type type, string memberName)
        {
            var sync = GetCoherenceSync(gameObject);
            _ = gameObject.TryGetComponent(type, out var component);

            return AddBinding(sync, component, memberName);
        }

        /// <inheritdoc cref="AddBinding(CoherenceSync,Component,Descriptor)" />
        public static Binding AddBinding(CoherenceSync sync, Component component, string memberName)
        {
            var descriptors = EditorCache.GetComponentDescriptors(component);
            var descriptor = descriptors.FirstOrDefault(d => d.Name == memberName);

            return AddBinding(sync, component, descriptor);
        }

        /// <summary>
        /// Adds a Binding to a CoherenceSync.
        /// </summary>
        public static Binding AddBinding(CoherenceSync sync, Component component, Descriptor descriptor)
        {
            ThrowIfInvalidBindingHandingArguments(sync, component, descriptor);

            var binding = sync.GetBindingForDescriptor(descriptor, component);
            if (binding != default)
            {
                return binding;
            }

            Undo.RecordObject(sync, "Add Binding");
            binding = descriptor.InstantiateBinding(component);

            try
            {
                OnBeforeBindingAdded?.Invoke(sync, binding);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            sync.Bindings.Add(binding);
            sync.ValidateArchetype();
            EditorUtility.SetDirty(sync);
            BakeUtil.CoherenceSyncSchemasDirty = true;

            return binding;
        }

        /// <inheritdoc cref="RemoveBinding(CoherenceSync,Component,string)"/>
        public static bool RemoveBinding<T>(GameObject gameObject, string memberName) where T : Component
        {
            var sync = GetCoherenceSync(gameObject);
            _ = gameObject.TryGetComponent(out T component);

            return RemoveBinding(sync, component, memberName);
        }

        /// <inheritdoc cref="RemoveBinding(CoherenceSync,Component,string)"/>
        public static bool RemoveBinding(GameObject gameObject, Type type, string memberName)
        {
            var sync = GetCoherenceSync(gameObject);
            _ = gameObject.TryGetComponent(type, out var component);

            return RemoveBinding(sync, component, memberName);
        }

        /// <inheritdoc cref="RemoveBinding(CoherenceSync,Component,Descriptor)"/>
        public static bool RemoveBinding(CoherenceSync sync, Component component, string memberName)
        {
            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            if (memberName == null)
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            // When there's no component reference, we check directly against the list of bindings on the CoherenceSync.
            // This is not strictly correct. We might be deleting a binding from a different component type,
            // since we only care about the member name. But since we don't have a component reference,
            // that's as good as it gets, while keeping usage simple.
            if (!component)
            {
                var binding = sync.Bindings.First(binding => binding.Descriptor.Name == memberName && !binding.UnityComponent);
                return RemoveBinding(sync, binding);
            }

            var descriptors = EditorCache.GetComponentDescriptors(component);
            var descriptor = descriptors.FirstOrDefault(d => d.Name == memberName);
            return RemoveBinding(sync, component, descriptor);
        }

        /// <inheritdoc cref="RemoveBinding(CoherenceSync,Binding)"/>
        public static bool RemoveBinding(CoherenceSync sync, Component component, Descriptor descriptor)
        {
            ThrowIfInvalidBindingHandingArguments(sync, component, descriptor);

            if (component)
            {
                var cachedDescriptors = EditorCache.GetComponentDescriptors(component);
                var cachedDescriptorIndex = cachedDescriptors.IndexOf(descriptor);
                // Instead of taking the provided descriptor argument for granted (serialized data), we want to know what's
                // the cached descriptor (the one that comes from reflecting what's found on the C# layout),
                // to check for the required flag.
                if (cachedDescriptorIndex != -1 && cachedDescriptors[cachedDescriptorIndex].Required)
                {
                    throw new DescriptorRequiredException($"{descriptor.Name} is a required binding and can't removed.");
                }
            }

            var binding = sync.GetBindingForDescriptor(descriptor, component) ?? sync.Bindings.FirstOrDefault(b => b.Descriptor.Name == descriptor.Name);
            if (binding == default)
            {
                return false;
            }

            return RemoveBinding(sync, binding);
        }

        /// <summary>
        /// Removes a Binding from a CoherenceSync.
        /// </summary>
        public static bool RemoveBinding(CoherenceSync sync, Binding binding)
        {
            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            Undo.RecordObject(sync, "Remove Binding");

            try
            {
                OnBeforeBindingRemoved?.Invoke(sync, binding);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (!sync.Bindings.Remove(binding))
            {
                return false;
            }

            sync.ValidateArchetype();
            EditorUtility.SetDirty(sync);
            BakeUtil.CoherenceSyncSchemasDirty = true;


            return true;
        }

        internal static (int totalCount, int invalidCount) GetVariableBindingsCount(CoherenceSync sync, GameObject context)
        {
            var totalCount = 0;
            var invalidCount = 0;

            for (var index = 0; index < sync.Bindings.Count; index++)
            {
                var binding = sync.Bindings[index];
                if (binding == null)
                {
                    continue;
                }

                if (binding.IsMethod)
                {
                    continue;
                }

                if (binding.unityComponent && binding.unityComponent.gameObject != context)
                {
                    continue;
                }

                totalCount++;

                if (!IsBindingValid(sync, index, out _))
                {
                    invalidCount++;
                }
            }

            return (totalCount, invalidCount);
        }

        public static (int totalCount, int invalidCount) GetMethodBindingsCount(CoherenceSync sync, GameObject context)
        {
            var totalCount = 0;
            var invalidCount = 0;

            for (var index = 0; index < sync.Bindings.Count; index++)
            {
                var binding = sync.Bindings[index];
                if (binding == null)
                {
                    continue;
                }

                if (binding.unityComponent && binding.unityComponent.gameObject != context)
                {
                    continue;
                }

                if (!binding.IsMethod)
                {
                    continue;
                }

                totalCount++;

                if (!IsBindingValid(sync, index, out _))
                {
                    invalidCount++;
                }
            }

            return (totalCount, invalidCount);
        }

        public static (int totalCount, int invalidCount) GetComponentActionsCount(CoherenceSync sync, GameObject context)
        {
            if (sync.componentActions is null)
            {
                return (0, 0);
            }

            var totalCount = 0;
            var invalidCount = 0;

            foreach (var componentAction in sync.componentActions)
            {
                if (componentAction is null)
                {
                    totalCount++;
                    invalidCount++;
                    continue;
                }

                if (!componentAction.component)
                {
                    totalCount++;
                    invalidCount++;
                    continue;
                }

                if (componentAction.component.gameObject == context)
                {
                    totalCount++;
                }
            }

            return (totalCount, invalidCount);
        }

        /// <summary>
        /// Should users and automated binding updating processes be allowed to edit bindings directly on the given
        /// <see cref="CoherenceSync"/> instance?
        /// <para>
        /// Editing of bindings is disallowed in the following contexts:
        /// <list type="number">
        /// <item>
        /// <description> The component is not part of a prefab asset nor open in a prefab stage. </description>
        /// </item>
        /// <item>
        /// <description> The component is part of a prefab instance. </description>
        /// </item>
        /// <item>
        /// <description> The component is being edited In Context in Prefab Mode. </description>
        /// </item>
        /// <item>
        /// <description>
        /// <see cref="CloneMode"/> is <see cref="CloneMode.Enabled">enabled</see>,
        /// and <see cref="interactionMode"/> is <see cref="InteractionMode.AutomatedAction"/>.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <see cref="CloneMode"/> is <see cref="CloneMode.Enabled">enabled</see>,
        /// and <see cref="CloneMode.AllowEdits"/> is <see langword="false"/>.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="sync"> The <see cref="CoherenceSync"/> component whose bindings' editability is determined. </param>
        /// <param name="interactionMode">
        /// Should editability be determined for a manual user interaction via a GUI, or for an automated process?
        /// </param>
        /// <returns></returns>
        internal static bool IsEditingBindingsAllowed(CoherenceSync sync, InteractionMode interactionMode)
        {
            var status = new GameObjectStatus(sync.gameObject);

            if (status
                is { IsAsset: false }
                or { IsInstanceInScene: true }
                or { IsNestedInstanceInsideAnotherPrefab: true }
                or { PrefabStageMode: PrefabStageMode.InContext })
            {
                return false;
            }

            if (CloneMode.Enabled)
            {
                // Automated modifications are never allowed in Clone Mode
                if (interactionMode == InteractionMode.AutomatedAction)
                {
                    return false;
                }

                // Manual modifications might be enabled in clone mode.
                if (!CloneMode.AllowEdits)
                {
                    return false;
                }
            }

            return true;
        }

        private static CoherenceSync GetCoherenceSync(GameObject gameObject)
        {
            if (!gameObject)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            var sync = gameObject.GetComponentInParent<CoherenceSync>(true);
            if (!sync)
            {
                throw new NotInHierarchyException(nameof(gameObject));
            }

            return sync;
        }

        private static void ThrowIfInvalidBindingHandingArguments(CoherenceSync sync, Component component, Descriptor descriptor)
        {
            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            if (descriptor == null)
            {
                throw new DescriptorNotFoundException();
            }

            if (component)
            {
                if (TypeUtils.IsNonBindableType(component.GetType()))
                {
                    throw new NonBindableException(component.GetType().FullName);
                }

                var transform = component.transform;
                var syncTransform = sync.transform;
                if (transform != syncTransform && !transform.IsChildOf(syncTransform))
                {
                    throw new NotInHierarchyException(nameof(component));
                }
            }
        }

        private static bool IsBindingUnrecoverable(CoherenceSync sync, Binding binding) => binding == null || binding.Descriptor == null || (binding.Descriptor.Required && !IsBindingValid(sync, binding, out _));

        internal static int RemoveUnrecoverableBindings(CoherenceSync sync)
        {
            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            return sync.Bindings.RemoveAll((binding) => IsBindingUnrecoverable(sync, binding));
        }

        /// <summary>
        /// Removes all invalid Bindings from a given <see cref="CoherenceSync"/>.
        /// </summary>
        /// <returns>Number of Bindings removed.</returns>
        /// <seealso cref="IsBindingValid(Coherence.Toolkit.CoherenceSync,int,out string)"/>
        public static int RemoveInvalidBindings(CoherenceSync sync)
        {
            var removed = 0;
            for (var index = 0; index < sync.Bindings.Count; index++)
            {
                if (IsBindingValid(sync, index, out _))
                {
                    continue;
                }

                if (RemoveBinding(sync, sync.Bindings[index]))
                {
                    removed++;
                    index--;
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes all Bindings from a given <see cref="CoherenceSync"/> that match the given predicate.
        /// </summary>
        /// <returns>Number of Bindings removed.</returns>
        public static int RemoveBindings(CoherenceSync sync, Predicate<Binding> match)
        {
            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            Undo.RecordObject(sync, "Remove Bindings");
            var count = sync.Bindings.RemoveAll(match);
                
            if (count > 0)
            {
                sync.ValidateArchetype();

                EditorUtility.SetDirty(sync);
                BakeUtil.CoherenceSyncSchemasDirty = true;
            }

            return count;
        }

        /// <summary>
        /// Destroys the <see cref="CoherenceSync"/> component for the given GameObject,
        /// and any other component that depends on it.
        /// </summary>
        /// <exception cref="ArgumentNullException">When <paramref name="gameObject"/> is <see langword="null"/>.</exception>
        public static void DestroyCoherenceComponents(GameObject gameObject)
        {
            if (!gameObject)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            // TODO find better way to deal with required components
            DestroyComponent<CoherenceNode>(gameObject);
            DestroyComponent<CoherenceInput>(gameObject);
            DestroyComponent<PrefabSyncGroup>(gameObject);
            DestroyComponent<CoherenceSync>(gameObject);
        }

        private static void DestroyComponent<T>(GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent(out T component))
            {
                Undo.DestroyObjectImmediate(component);
            }
        }

        /// <summary>
        /// Updates Binding Descriptors, adds missing required Bindings,
        /// and removes invalid Bindings.
        /// </summary>
        /// <remarks>
        /// When creating <see cref="CoherenceSync"/> components from code,
        /// you must call this method to ensure Bindings are up-to-date.
        /// </remarks>
        /// <param name="sync">The target component.</param>
        /// <returns>
        /// <see langword="true"/> if the component gets modified; <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sync"/> is <see langword="null"/>.</exception>
        /// <seealso cref="Binding"/>
        /// <seealso cref="Descriptor"/>
        public static bool UpdateBindings(CoherenceSync sync) => EditorCache.UpdateBindings(sync);

        internal static bool UpdateBindings(CoherenceSync sync, bool forceUpdate) => EditorCache.UpdateBindings(sync, forceUpdate);

        /// <summary>
        /// Adds a <see cref="CoherenceSync"/> component and converts a GameObject into a Prefab.
        /// Prefabs using CoherenceSync are ready to be networked by coherence.
        /// </summary>
        /// <param name="gameObject">GameObject to convert into a Prefab.</param>
        /// <param name="prefabPath">Path where the Prefab Asset will be written on disk.</param>
        /// <param name="prefab">Reference to the Prefab Asset.</param>
        /// <param name="interactionMode">Indicates whether the user should be prompted for confirmation or performed automatically.</param>
        /// <returns><see langword="true"/> if the Prefab Asset gets created; <see langword="false"/> otherwise.</returns>
        public static bool TryConvertToCoherenceSyncPrefab(GameObject gameObject, string prefabPath,
            out GameObject prefab, InteractionMode interactionMode = InteractionMode.UserAction)
        {
            prefab = default;

            if (!gameObject)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            if (!gameObject.TryGetComponent(out CoherenceSync sync))
            {
                sync = Undo.AddComponent<CoherenceSync>(gameObject);
            }

            var status = new GameObjectStatus(gameObject);
            bool success;
            if (status.IsAsset)
            {
                if (status.IsInPrefabStage)
                {
                    success = true;
                }
                else if (PrefabUtility.IsAddedComponentOverride(sync))
                {
                    PrefabUtility.ApplyAddedComponent(sync, prefabPath, interactionMode);
                    success = true;
                }
                else
                {
                    prefab = PrefabUtility.SavePrefabAsset(gameObject, out success);
                }
            }
            else
            {
                if (status.PrefabInstanceStatus == PrefabInstanceStatus.NotAPrefab)
                {
                    prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
                }
                else
                {
                    PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.OutermostRoot, interactionMode);
                }

                prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, prefabPath, interactionMode, out success);
            }

            return success;
        }

        /// <summary>
        /// Check if a binding on a CoherenceSync component is valid.
        /// </summary>
        /// <remarks>
        /// A binding is valid if:
        /// 1. The target Component exists.
        /// 2. The associated Descriptor for the given Component type exists.
        ///
        /// <see cref="IsBindingValid(Coherence.Toolkit.CoherenceSync,int,out string)"/> does additional validity checks
        /// that can only be performed knowing the specific binding index on <see cref="CoherenceSync.Bindings"/>.
        /// </remarks>
        /// <param name="sync">The CoherenceSync component the binding is on.</param>
        /// <param name="binding">The Binding instance that represents a synced member.</param>
        /// <param name="invalidReason">If the binding is invalid, this string holds the reason why.</param>
        /// <returns>
        /// <see langword="true"/> if the binding is valid; <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsBindingValid(CoherenceSync sync, Binding binding, out string invalidReason)
        {
            invalidReason = string.Empty;

            // We check if the given Binding is null
            if (binding == null)
            {
                invalidReason = "Binding is null.";
                return false;
            }

            // We check if the associated Component is missing
            if (binding.UnityComponent == null)
            {
                invalidReason = "Binding is missing a valid Component reference.";
                return false;
            }

            // We check if the associated Descriptor is missing
            if (binding.Descriptor == null)
            {
                invalidReason = "Binding Descriptor is null.";
                return false;
            }

            // We check if the Binding is pointing to a component from another Prefab
            if (!IsPartOfHierarchy(binding.UnityComponent, sync))
            {
                invalidReason = "Binding is pointing to a Component from another prefab.";
                return false;
            }

            var isValidResult = binding.IsBindingValid();
            if (!isValidResult.IsValid
            // Ignore invalid results from bindings that are required to be attached to root objects,
            // when they are attached to the root of some prefab asset. Otherwise, they will give false positives
            // in situations that are actually supported - like when nesting coherence sync objects and using a PrefabSyncGroup.
            && (binding is not (PositionBinding or RotationBinding or ScaleBinding) || !IsRootObject(sync)))
            {
                invalidReason = isValidResult.Reason;
                return false;
            }

            // We check if the associated Descriptor is no longer part of the associated Component
            var descriptorsForComponent = EditorCache.GetComponentDescriptors(binding.UnityComponent);

            var result = EditorCache.DescriptorExistsForBinding(descriptorsForComponent, binding);

            if (!result)
            {
                invalidReason = "No descriptor matches the serialized data.\n\n" +
                                "Possible reasons:\n" +
                                "  Deleted or renamed member.\n" +
                                "  Changed member signature.\n" +
                                "  Renamed class or namespace.\n" +
                                "  Moved class to a different namespace.";
            }

            return result;
        }

        /// <summary>
        /// Check if a binding on a CoherenceSync component is valid.
        /// </summary>
        /// <remarks>
        /// A binding is valid if:
        /// 1. The target Component exists.
        /// 2. The associated Descriptor for the given Component type exists.
        /// </remarks>
        /// <param name="sync">The CoherenceSync component the binding is on.</param>
        /// <param name="index">The index on <see cref="CoherenceSync.Bindings"/> for the binding to validate.</param>
        /// <param name="invalidReason">If the binding is invalid, this string holds the reason why.</param>
        /// <returns>
        /// <see langword="true"/> if the binding is valid; <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsBindingValid(CoherenceSync sync, int index, out string invalidReason)
        {
            var binding = sync.Bindings[index];
            if (!IsBindingValid(sync, binding, out invalidReason))
            {
                return false;
            }

            // Bindings are registered in the order they are found in CoherenceSync.Bindings.
            // In case of duplicated bindings/descriptors, if the binding is not the first one found, it is considered invalid.
            var firstIndex = sync.Bindings.FindIndex(b => b.UnityComponent == binding.UnityComponent && b.Descriptor == binding.Descriptor);
            if (firstIndex != -1 && firstIndex != index)
            {
                invalidReason = "Binding is a duplicate.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a value indicating whether the object is a root object which should use shallow bindings like <see cref="PositionBinding"/>,
        /// like deep bindings, like <see cref="DeepPositionBinding"/>.
        /// </summary>
        internal static bool IsRootObject(Component component)
        {
            if (!component.transform.parent)
            {
                return true;
            }

            var prefabStage = PrefabStageUtility.GetPrefabStage(component.gameObject);
            if (prefabStage && prefabStage.prefabContentsRoot == component.gameObject)
            {
                return true;
            }

            if (PrefabUtility.IsAnyPrefabInstanceRoot(component.gameObject))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether a given Prefab Instance contains overrides that might impede networking.
        /// Reasons why such overrides may occur are diverse: from manual editing (via script or Inspector debug mode)
        /// to legacy implementations mistakenly modifying them.
        /// </summary>
        /// <param name="sync">CoherenceSync component that's part of a Prefab Instance.</param>
        /// <returns>
        /// <see langword="true"/> if the component has undesired overrides; <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sync"/> is <see langword="null"/>.</exception>
        /// <seealso cref="RemoveUndesiredOverrides"/>
        public static bool HasUndesiredOverrides(CoherenceSync sync)
        {
            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            if (new GameObjectStatus(sync.gameObject)
                is not { PrefabInstanceStatus: PrefabInstanceStatus.Connected }
                // Prefab instance at the root of the prefab stage must be a prefab variant,
                // in which case all property modifications are allowed.
                or { IsRootOfPrefabStageHierarchy: true })
            {
                return false;
            }

            var propertyModifications = PrefabUtility.GetPropertyModifications(sync);
            return propertyModifications is not null && Array.Exists(propertyModifications, IsUndesiredOverride);
        }

        /// <summary>
        /// Checks whether a given property modification is one that might impede networking.
        /// Reasons why such overrides may occur are diverse: from manual editing (via script or Inspector debug mode)
        /// to legacy implementations mistakenly modifying them.
        /// </summary>
        /// <param name="propertyModification"> Property modification from a prefab instance to check. </param>
        /// <returns>
        /// <see langword="true"/> if the property modification is undesired; otherwise, <see langword="false"/>.
        /// </returns>
        /// <seealso cref="HasUndesiredOverrides"/>
        private static bool IsUndesiredOverride(PropertyModification propertyModification)
        {
            if (propertyModification.target is not CoherenceSync)
            {
                return false;
            }

            var modificationPath = propertyModification.propertyPath;
            foreach (var undesiredPath in UndesiredOverrides)
            {
                if (!modificationPath.StartsWith(undesiredPath))
                {
                    continue;
                }

                // A guarded property has been modified directly.
                // E.g. "coherenceSyncConfig" => "coherenceSyncConfig".
                if (modificationPath.Length == undesiredPath.Length ||
                // A property nested inside a guarded property has been modified.
                // E.g. "bindings" -> "bindings.Array.size"
                // E.g. "managedReferences" => managedReferences[441425684606484485].isPredicted"
                modificationPath[undesiredPath.Length] is '.' or '[')
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes overrides from a Prefab Instance that might impede networking.
        /// Reasons why such overrides may occur are diverse: from manual editing (via script or Inspector debug mode)
        /// to legacy implementations mistakenly modifying them.
        /// </summary>
        /// <param name="sync">CoherenceSync component that's part of a Prefab Instance.</param>
        /// <param name="interactionMode">Indicates whether the user should be prompted for confirmation or performed automatically.</param>
        /// <returns>
        /// Number of removed overrides.
        /// </returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sync"/> is <see langword="null"/>.</exception>
        /// <seealso cref="HasUndesiredOverrides"/>
        public static int RemoveUndesiredOverrides(CoherenceSync sync, InteractionMode interactionMode = InteractionMode.AutomatedAction)
        {
            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            var status = PrefabUtility.GetPrefabInstanceStatus(sync);
            if (status != PrefabInstanceStatus.Connected)
            {
                return 0;
            }

            var propertyModifications = PrefabUtility.GetPropertyModifications(sync.gameObject) ?? Array.Empty<PropertyModification>();
            var propertyModificationsList = propertyModifications.ToList();
            var removedCount = propertyModificationsList.RemoveAll(IsUndesiredOverride);
            if (removedCount == 0)
            {
                return removedCount;
            }

            bool perform;
            if (interactionMode == InteractionMode.UserAction)
            {
                using var serializedObject = new SerializedObject(sync);

                var undesiredOverrides = propertyModifications.Where(IsUndesiredOverride);

                perform = EditorUtility.DisplayDialog("Fix Prefab Instance",
                "The following changes to this prefab instance might make networking fail:\n\n" +
                string.Join("\n", undesiredOverrides.Select(GetDisplayNameForProperty)) +
                "\n\nDo you want to revert these changes to match the parent prefab?", "OK (Recommended)", "Cancel");

                string GetDisplayNameForProperty(PropertyModification modification)
                {
                    var propertyPath = modification.propertyPath;

                    const string ArrayDataPrefix = ".Array.data[";
                    const string ArraySizeSuffix = ".Array.size";
                    const string BindingPathPrefix = Property.bindings + ArrayDataPrefix;
                    if (TryGetIndex(propertyPath, BindingPathPrefix, out var indexInList))
                    {
                        if (indexInList >= sync.Bindings.Count)
                        {
                            return "Missing Indexed Reference";
                        }

                        if (GetDisplayNameForBinding(sync.Bindings[indexInList], out var displayName))
                        {
                            return displayName;
                        }
                    }

                    const string BindingsCountPath = Property.bindings + ArraySizeSuffix;
                    if (string.Equals(propertyPath, BindingsCountPath))
                    {
                        return
                            $"The number of bindings has changed. This is usually caused by changes to a script file on the parent prefab. The 'Bindings' collection has been resized to {modification.value}.\n";
                    }

                    const string ComponentActionPathPrefix = Property.componentActions + ArrayDataPrefix;
                    if (TryGetIndex(propertyPath, ComponentActionPathPrefix, out var indexInArray))
                    {
                        if (GetDisplayNameForBinding(sync.Bindings[indexInArray], out var displayName))
                        {
                            return displayName;
                        }
                    }

                    const string ComponentActionLengthPath = Property.bindings + ArraySizeSuffix;
                    if (string.Equals(propertyPath, ComponentActionLengthPath))
                    {
                        return "Component Actions Resized to " + modification.value;
                    }

                    const string archetypePathPrefix = Property.archetype + ".boundComponents.Array.data[";

                    if (serializedObject.FindProperty(propertyPath) is SerializedProperty property)
                    {
                        if (property.displayName == "Component" && TryGetIndex(propertyPath, archetypePathPrefix, out var archetypeIndex))
                        {
                            var parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(sync);
                            return GetComponentName(parentPrefab, archetypeIndex);
                        }
                        else
                        {
                            return property.displayName;
                        }
                    }

                    if (TryGetIndex(propertyPath, archetypePathPrefix, out var bindingIndex))
                    {
                        return bindingIndex >= sync.Archetype.BoundComponents.Count
                            ? GetArchetypeNameFromParent(sync, bindingIndex)
                            : $"{sync.Archetype.BoundComponents[bindingIndex].DisplayName}";
                    }

                    if (propertyPath.StartsWith(managedReferencePath))
                    {
                        var idStart = managedReferencePath.Length + 1;
                        var idEnd = propertyPath.IndexOf(']', idStart);
                        if (long.TryParse(propertyPath.Substring(idStart, idEnd - idStart), out var managedReferenceId))
                        {
                            var bindingsProperty = serializedObject.FindProperty(Property.bindings);
                            for (int i = 0, count = sync.Bindings.Count; i < count; i++)
                            {
                                var bindingProperty = bindingsProperty.GetArrayElementAtIndex(i);
                                if (bindingProperty.managedReferenceId == managedReferenceId)
                                {
                                    if (GetDisplayNameForBinding(sync.Bindings[i], out var displayName))
                                    {
                                        return displayName;
                                    }
                                }
                            }

                            var componentActionsProperty = serializedObject.FindProperty(Property.bindings);
                            for (int i = 0, count = sync.componentActions.Length; i < count; i++)
                            {
                                var componentActionProperty = componentActionsProperty.GetArrayElementAtIndex(i);
                                if (componentActionProperty.managedReferenceId == managedReferenceId)
                                {
                                    var componentAction = sync.componentActions[i];
                                    if (componentAction is null)
                                    {
                                        return "Missing Component Action";
                                    }

                                    return ObjectNames.NicifyVariableName(componentAction.GetType().Name);
                                }
                            }

                            var parentPrefab = PrefabUtility.GetCorrespondingObjectFromSource(sync.gameObject);
                            if (parentPrefab is not null)
                            {
                                var parentPrefabSync = parentPrefab.GetComponent<CoherenceSync>();
                                var serializedParentPrefab = new SerializedObject(parentPrefabSync);
                                var boundProperty = serializedParentPrefab.FindProperty(Property.bindings);
                                for (int i = 0, count = parentPrefabSync.Bindings.Count; i < count; i++)
                                {
                                    var bindingProperty = boundProperty.GetArrayElementAtIndex(i);
                                    if (bindingProperty.managedReferenceId == managedReferenceId)
                                    {
                                        if (GetDisplayNameForBinding(parentPrefabSync.Bindings[i], out var displayName))
                                        {
                                            return displayName;
                                        }
                                    }
                                }
                            }
                        }

                        return "Missing Managed Reference";
                    }

                    return propertyPath;
                }
            }
            else
            {
                perform = true;
            }

            if (!perform)
            {
                return 0;
            }

            Undo.RecordObject(sync, "Remove Undesired CoherenceSync Overrides");

            // Get the list of removed components and then re-add them to their corresponding
            // game objects
            var removedComponents = PrefabUtility.GetRemovedComponents(sync.gameObject);
            foreach (var removedComponent in removedComponents)
            {
                PrefabUtility.RevertRemovedComponent(removedComponent.containingInstanceGameObject, removedComponent.assetComponent,
                    InteractionMode.AutomatedAction);
            }

            PrefabUtility.SetPropertyModifications(sync, propertyModificationsList.ToArray());
            return removedCount;
        }

        private static string GetComponentName(CoherenceSync parentPrefab, int archetypeIndex)
        {
            if (parentPrefab == null)
            {
                return "Missing Bound Component";
            }

            var parentPrefabSync = parentPrefab.GetComponent<CoherenceSync>();
            if (parentPrefabSync == null)
            {
                return "Missing Bound Component";
            }

            if (archetypeIndex >= parentPrefabSync.Archetype.BoundComponents.Count)
            {
                return "Missing Bound Component";
            }

            return $"{parentPrefabSync.Archetype.BoundComponents[archetypeIndex].DisplayName} (Component)";
        }

        private static string GetArchetypeNameFromParent(CoherenceSync childSync, int bindingIndex)
        {
            var archetypeName = "Missing Bound Component";

            var parentPrefab = PrefabUtility.GetCorrespondingObjectFromSource(childSync.gameObject);
            if (parentPrefab is not null)
            {
                var parentPrefabSync = parentPrefab.GetComponent<CoherenceSync>();

                archetypeName = bindingIndex >= parentPrefabSync.Archetype.BoundComponents.Count
                    ? "Missing Bound Component"
                    : $"{parentPrefabSync.Archetype.BoundComponents[bindingIndex].DisplayName}";
            }

            return archetypeName;
        }

        private static bool GetDisplayNameForBinding([AllowNull] Binding binding, out string displayName)
        {
            if (binding is null || !binding.IsValid)
            {
                displayName = "Missing Binding";
                return false;
            }

            if (binding.SignaturePlainText is { Length: > 0 } signature)
            {
                displayName = $"{binding.unityComponent.GetType().Name}.{signature}";
                return true;
            }

            displayName = ObjectNames.NicifyVariableName(binding.GetType().Name);
            return true;
        }

        private static bool TryGetIndex(string source, string prefix, out int index)
        {
            index = -1;
            return source.StartsWith(prefix)
                   && source.IndexOf(']',
                           prefix.Length + 1) is
                       var archetypeIndexEnd and not -1
                   && int.TryParse(
                       source.Substring(prefix.Length,
                           archetypeIndexEnd - prefix.Length),
                       out index);
        }

        private static bool IsPartOfHierarchy(Component c, CoherenceSync sync)
        {
            if (!c)
            {
                return false;
            }

            var root = sync.transform;
            var t = c.transform;
            do
            {
                if (t == root)
                {
                    return true;
                }
            }
            while (t = t.parent);

            return false;
        }
    }
}
