// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Bindings;
    using Bindings.TransformBindings;
    using Editor;
    using Editor.Toolkit.BindingProviders;
    using Unity.Profiling;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class EditorCache
    {
        private static readonly Dictionary<Type, (Type, int)> bindingProviderTypeCache = new();
        private static readonly Dictionary<Component, DescriptorProvider> bindingProviderInstanceCache = new();
        private static readonly List<Component> cachedComponents = new();

        // key: component type
        // value: component action type
        private static readonly List<KeyValuePair<Type, Type>> componentActionTypeCache = new();

        // key: component action type
        // value: display name of the component action
        private static readonly Dictionary<Type, string> componentActionNameCache = new();

        // key: component type
        // value: available component actions for the given component
        private static readonly Dictionary<Type, List<Type>> componentActionCache = new();

        private static readonly Dictionary<Descriptor, GUIContent> descriptorContents = new();
        private static readonly List<CoherenceSync> cachedSyncs = new();
        private static readonly List<CoherenceSync> changedSyncs = new();

        private static readonly ProfilerMarker updateBindingsMarker = new("Coherence.UpdateBindings");

        static EditorCache()
        {
            RegisterBindingProviders();
            RegisterComponentActionTypes();
            RegisterConditionallyBakedTypes();
        }

        private static void RegisterConditionallyBakedTypes()
        {
            const string settingsKey = "Coherence.BakeConditionalsHash";

            try
            {
                var componentsWithBakeConditional = TypeCache.GetTypesWithAttribute<BakeConditionalAttribute>();
                Hash128 hash = default;

                foreach (var type in componentsWithBakeConditional.OrderBy(t => t.AssemblyQualifiedName))
                {
                    var conditional = type.GetCustomAttribute<BakeConditionalAttribute>();
                    hash.Append(conditional.Condition);
                    hash.Append(type.AssemblyQualifiedName);
                }

                var lastHash = UserSettings.GetString(settingsKey, "");
                var newHashCode = hash.ToString();

                if (lastHash != newHashCode)
                {
                    BakeUtil.CoherenceSyncSchemasDirty = true;
                    UserSettings.SetString(settingsKey, newHashCode);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to register conditionally baked types: {exception}");
            }
        }

        [DidReloadScripts]
        internal static void UpdateBindingsAndNotify()
        {
            if (CloneMode.Enabled)
            {
                return;
            }

            // If Asset Database is updating we have to delay the version update call, to make sure the latest versions of the assets are imported.
            if (EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += UpdateNetworkPrefabs;
                return;
            }

            UpdateNetworkPrefabs();
        }

        private static void UpdateNetworkPrefabs()
        {
            if (Application.isPlaying)
            {
                return;
            }

            CoherenceSyncConfigRegistry.Instance.GetReferencedPrefabs(cachedSyncs);

            changedSyncs.Clear();
            var changed = false;
            changed |= NetworkPrefabProcessor.UpdateNetworkPrefabs(cachedSyncs, changedSyncs);
            changed |= UpdateAllCoherenceSyncResourcesPrefabsBindings(cachedSyncs, changedSyncs);

            if (changed)
            {
                EditorApplication.delayCall += NotifyCoherenceSyncChanges;
            }
        }

        private static void NotifyCoherenceSyncChanges()
        {
            var stringBuilder = new StringBuilder("Prefabs using CoherenceSync have been updated to reflect changes:");

            var max = 12;
            for (var i = 0; i < changedSyncs.Count; i++)
            {
                if (i >= max)
                {
                    stringBuilder.Append($"\n({changedSyncs.Count - max} more)");
                    break;
                }

                stringBuilder.Append("\n");
                stringBuilder.Append(changedSyncs[i].name);
            }

            if (EditorUtility.scriptCompilationFailed)
            {
                if (Application.isBatchMode)
                {
                    stringBuilder.Append("\nHowever, there's compiler errors, so coherence won't automatically save changes to disk.");
                    Debug.LogWarning(stringBuilder);
                }
                else
                {
                    stringBuilder.Append("\nWARNING: There's compiler errors, so coherence won't automatically save changes to disk.");
                    _ = EditorUtility.DisplayDialog("CoherenceSync Prefabs Updated",
                        stringBuilder.ToString(),
                        "Ok, I will save changes myself");
                }
            }
            else
            {
                if (Application.isBatchMode)
                {
                    stringBuilder.Append("\nChanges saved to disk.");
                    Debug.Log(stringBuilder);
                }
                else
                {
                    stringBuilder.Append("\nChanges will be saved to disk.");
                    _ = EditorUtility.DisplayDialog("CoherenceSync Prefabs Updated",
                        stringBuilder.ToString(),
                        "Ok, save changes");
                }

                AssetDatabase.StartAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void RegisterBindingProviders()
        {
            var providerTypes = TypeCache.GetTypesWithAttribute<DescriptorProviderAttribute>();
            foreach (var providerType in providerTypes)
            {
                var attr = providerType.GetCustomAttribute<DescriptorProviderAttribute>();
                if (bindingProviderTypeCache.TryGetValue(attr.componentType, out (Type t, int prio) tuple))
                {
                    if (attr.priority > tuple.prio)
                    {
                        bindingProviderTypeCache[attr.componentType] = (providerType, attr.priority);
                    }
                    else if (attr.priority == tuple.prio)
                    {
                        Debug.LogWarning(
                            $"BinderProviders {providerType.Name} and {tuple.t.Name} (operate on {attr.componentType.Name}) have same priority. Please, use a different priority.");
                    }
                }
                else
                {
                    bindingProviderTypeCache.Add(attr.componentType, (providerType, attr.priority));
                }
            }
        }

        private static void RegisterComponentActionTypes()
        {
            componentActionTypeCache.Clear();
            componentActionNameCache.Clear();
            var types = TypeCache.GetTypesWithAttribute<ComponentActionAttribute>();
            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes<ComponentActionAttribute>(false);
                if (!type.IsSubclassOf(typeof(ComponentAction)))
                {
                    Debug.LogWarning(
                        $"{type.Name} uses the ComponentAction attribute but does not inherit from ComponentAction.");
                    continue;
                }

                foreach (var attr in attrs)
                {
                    if (attr.componentType == null)
                    {
                        Debug.LogWarning($"{type.Name} uses a ComponentAction with a null type.");
                        continue;
                    }

                    if (!typeof(Component).IsAssignableFrom(attr.componentType))
                    {
                        Debug.LogWarning(
                            $"{type.Name} uses a ComponentAction attribute with the incompatible type {attr.componentType}.");
                        continue;
                    }

                    componentActionTypeCache.Add(new KeyValuePair<Type, Type>(attr.componentType, type));
                    componentActionNameCache.Add(type, attr.name);
                }
            }
        }

        public static GUIContent GetContent(Component component, Descriptor descriptor)
        {
            if (!descriptorContents.TryGetValue(descriptor, out var content))
            {
                if (GetBindingProviderForComponent(component, out var provider))
                {
                    content = new GUIContent(
                        $"{(descriptor.Required && descriptor.BindingType != typeof(PositionBinding) ? $"[{(descriptor.IsMethod ? "Command" : "Sync")}] " : string.Empty)}{descriptor.Signature}",
                        provider.GetIconContent(descriptor).image);
                }
                else
                {
                    content = new GUIContent(descriptor.Signature, "Couldn't load associated BindingProvider");
                }

                descriptorContents.Add(descriptor, content);
            }

            return content;
        }

        private static bool UpdateAllCoherenceSyncResourcesPrefabsBindings(List<CoherenceSync> syncs, List<CoherenceSync> syncsChanged)
        {
            var changed = false;

            foreach (var sync in syncs)
            {
                if (UpdateBindings(sync))
                {
                    syncsChanged.Add(sync);
                    changed = true;
                }
            }

            return changed;
        }

        public static List<Descriptor> GetComponentDescriptors(Component component)
        {
            if (!GetBindingProviderForComponent(component, out DescriptorProvider provider))
            {
                return new List<Descriptor>();
            }

            return provider.Fetch();
        }

        public static bool DescriptorExistsForBinding(List<Descriptor> descriptors, Binding binding)
        {
            foreach (var descriptor in descriptors)
            {
                if (descriptor == null || binding == null)
                {
                    continue;
                }

                if (descriptor == binding.Descriptor)
                {
                    return true;
                }
            }

            return false;
        }

        private static DescriptorProvider GetDescriptorProvider(Type type)
        {
            if (type == typeof(DescriptorProvider))
            {
                return new DescriptorProvider();
            }

            if (type == typeof(TransformDescriptorProvider))
            {
                return new TransformDescriptorProvider();
            }

            if (type == typeof(RectTransformDescriptorProvider))
            {
                return new RectTransformDescriptorProvider();
            }

            return Activator.CreateInstance(type) as DescriptorProvider;
        }

        public static bool GetBindingProviderForComponent(Component component, out DescriptorProvider provider)
        {
            if (!component)
            {
                provider = null;
                return false;
            }

            if (bindingProviderInstanceCache.TryGetValue(component, out provider))
            {
                return true;
            }

            if (!bindingProviderTypeCache.TryGetValue(component.GetType(), out (Type providerType, int prio) tuple))
            {
                tuple.providerType = typeof(DescriptorProvider);
            }

            provider = GetDescriptorProvider(tuple.providerType);
            provider.SetComponent(component);

            // --- DANGER ZONE ---
            // Another Unity 2020 bug workaround.
            // 1. After starting Unity, and before code recompilation,
            //    references to existing components fail to deserialize
            //    and are incorrectly seen as null.
            //    This happens even when using any of the GetComponent methods.
            // 2. Using GameObject references *everywhere* instead, makes it work.
            // 3. GetComponentInParent has to use the overload which takes a bool
            //    otherwise it will return actual "true" (non-Unity) null ^^
            provider.Root = component.gameObject.GetComponentInParent<CoherenceSync>(true).gameObject;
            // --- END OF DANGER ZONE ---

            bindingProviderInstanceCache[component] = provider;

            return true;
        }

        public static string GetComponentActionName<T>() where T : ComponentAction
        {
            return componentActionNameCache.TryGetValue(typeof(T), out string name)
                ? string.IsNullOrEmpty(name) ? typeof(T).Name : name
                : typeof(T).Name;
        }

        public static string GetComponentActionName(Type componentActionType)
        {
            return componentActionNameCache.TryGetValue(componentActionType, out string name)
                ? string.IsNullOrEmpty(name) ? componentActionType.Name : name
                : componentActionType.Name;
        }

        public static string GetComponentActionName(ComponentAction componentAction)
        {
            return componentAction != null ? GetComponentActionName(componentAction.GetType()) : "<missing>";
        }

        public static bool GetComponentActionsForComponent(Component component, out List<Type> actionTypes)
        {
            if (!component)
            {
                actionTypes = null;
                return false;
            }

            if (!componentActionCache.TryGetValue(component.GetType(), out actionTypes))
            {
                actionTypes = new List<Type>();
                foreach (var kvp in componentActionTypeCache)
                {
                    var type = component.GetType();
                    if (kvp.Key.IsAssignableFrom(type))
                    {
                        actionTypes.Add(kvp.Value);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Updates bindings on <see cref="CoherenceSync"/> component, updating binding descriptors,
        /// adding missing required bindings, and removing invalid Bindings.
        /// <para>
        /// If editing the bindings is not <see cref="CoherenceSyncUtils.IsEditingBindingsAllowed">allowed</see>
        /// then the method won't make any changes to the bindings and returns <see cref="false"/>.
        /// </para>
        /// </summary>
        /// <param name="sync"> The <see cref="CoherenceSync"/> component that contains the bindings.  </param>
        /// <param name="forceUpdate">
        /// If set to <see langword="true"/>, then bindings will be updated even if
        /// <see cref="CoherenceSyncUtils.IsEditingBindingsAllowed"/> says this should not be allowed.
        /// </param>
        /// <returns> <see langword="true"/> if any bindings were changed; otherwise, <see langword="false"/>. </returns>
        /// <exception cref="ArgumentNullException">When <paramref name="sync"/> is <see langword="null"/>.</exception>
        public static bool UpdateBindings(CoherenceSync sync, bool forceUpdate = false)
        {
            using var marker = updateBindingsMarker.Auto();

            if (!sync)
            {
                throw new ArgumentNullException(nameof(sync));
            }

            if(!CoherenceSyncUtils.IsEditingBindingsAllowed(sync, InteractionMode.AutomatedAction) && !forceUpdate)
            {
                return false;
            }

            var changed = false;

            foreach (var binding in sync.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (Guid.TryParse(binding.guid, out var guid))
                {
                    var guidString = guid.ToString("N");
                    if (binding.guid != guidString)
                    {
                        binding.guid = guidString;
                        changed = true;
                    }
                }
            }

            var isVariant = new GameObjectStatus(sync.gameObject).IsVariant;
            var limitVariantPostProcessing = isVariant && !OverrideActions.HasBindingOverrides(sync);
            List<AddedComponent> addedComponents = null;
            List<AddedGameObject> addedGameObjects = null;

            if (limitVariantPostProcessing)
            {
                addedComponents = PrefabUtility.GetAddedComponents(sync.gameObject);
                addedGameObjects = PrefabUtility.GetAddedGameObjects(sync.gameObject);
            }

            if (!limitVariantPostProcessing)
            {
                GetSyncComponents(sync, cachedComponents);
            }
            else
            {
                GetUniqueComponentsForVariant(cachedComponents, addedComponents, addedGameObjects);
            }

            foreach (var component in cachedComponents)
            {
                if (!component)
                {
                    continue;
                }

                if (SkipComponentIfBelongsToAnotherSync(sync, component))
                {
                    continue;
                }

                var descriptors = GetComponentDescriptors(component);

                foreach (var descriptor in descriptors)
                {
                    if (descriptor == null)
                    {
                        continue;
                    }

                    if (descriptor.MemberType is MemberTypes.Field or MemberTypes.Property or MemberTypes.Method)
                    {
                        var changedBinding = sync.ShouldUpdateBindingDescriptor(descriptor, component);

                        if (changedBinding != null)
                        {
                            LogChange(sync, changedBinding);
                            _ = sync.Bindings.Remove(changedBinding);
                            _ = CoherenceSyncUtils.AddBinding(sync, component, descriptor);
                            changed = true;
                            continue;
                        }
                    }

                    if (descriptor.Required)
                    {
                        if (!sync.HasBindingForDescriptor(descriptor, component))
                        {
                            _ = CoherenceSyncUtils.AddBinding(sync, component, descriptor);
                            changed = true;
                        }

                        continue;
                    }

                    var binding = sync.GetBindingForDescriptor(descriptor, component);

                    if (binding == null || !binding.Descriptor.Required)
                    {
                        continue;
                    }

                    CoherenceSyncUtils.RemoveBinding(sync, component, binding.Descriptor);
                    changed = true;
                }
            }

            changed |= CoherenceSyncUtils.RemoveUnrecoverableBindings(sync) > 0;
            changed |= sync.ValidateArchetype();

            if (changed)
            {
                EditorUtility.SetDirty(sync);
                BakeUtil.CoherenceSyncSchemasDirty = true;
            }

            return changed;
        }

        private static void GetSyncComponents(CoherenceSync sync, List<Component> components) => sync.gameObject.GetComponentsInChildren(true, components);

        private static void GetUniqueComponentsForVariant(List<Component> components, List<AddedComponent> addedComponents,
            List<AddedGameObject> addedGameObjects)
        {
            components.Clear();

            // For Variants with no binding overrides, we only want to postprocess Components that do not exist in the base Prefab.
            foreach (var addedComponent in addedComponents)
            {
                components.Add(addedComponent.instanceComponent);
            }

            foreach (var addedObj in addedGameObjects)
            {
                components.AddRange(addedObj.instanceGameObject.GetComponentsInChildren(typeof(Component), true));
            }
        }

        private static bool SkipComponentIfBelongsToAnotherSync(CoherenceSync sync, Component component)
        {
            var prefabInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(component);

            if (prefabInstanceRoot == null)
            {
                return false;
            }

            var instanceSync = prefabInstanceRoot.GetComponent<CoherenceSync>();

            var shouldSkipThisComponent = instanceSync != null && instanceSync != sync &&
                                          instanceSync.CoherenceSyncConfig != null &&
                                          instanceSync.CoherenceSyncConfig != sync.CoherenceSyncConfig;

            return shouldSkipThisComponent;
        }

        private static void LogChange(CoherenceSync sync, Binding changedBinding)
        {
            var message = $"Binding with name: {changedBinding.Descriptor.Name} in {sync.gameObject.name} " +
                          "has been automatically updated. Updating the prefab.";

            Debug.Log(message);
        }
    }
}
