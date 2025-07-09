// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Archetypes;
    using Coherence.Toolkit.Bindings;
    using Log;
    using Portal;
    using UnityEditor;
    using UnityEngine;
    using Logger = Log.Logger;
    using Object = UnityEngine.Object;

    internal class InvalidCoherenceSyncException : Exception
    {
        internal InvalidCoherenceSyncException(string message) : base(message)
        {
        }
    }

    internal class SchemaCreator
    {
        private static readonly Logger logger = Log.GetLogger<SchemaCreator>();

        private static int MaxArchetypeLodComponents = 32;

        private static string GetValueOrDefault(string v, string d)
        {
            return !string.IsNullOrEmpty(v) ? v : d;
        }

        public static bool GatherSyncBehavioursAndEmit(out SchemaDefinition schemaDef,
            out EntitiesBakeData entitiesData)
        {
            schemaDef = null;
            entitiesData = default;

            logger.Debug("Generating Schema file...");

            File.Delete(Path.GetFullPath(Paths.gatherSchemaPath));
            ProjectSettings.instance.PruneSchemas();
            BakeUtil.CoherenceSyncSchemasDirty = false;

            try
            {
                var data = SaveSchemaAndJson();
                schemaDef = data.schemaDefinition;
                entitiesData = data.entitiesData;
                SaveBundledSchema();

                logger.Debug($"Finished generating Schema file: {Schemas.GetLocalSchemaID()}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                ProjectSettings.instance.PruneSchemas();
                BakeUtil.CoherenceSyncSchemasDirty = false;
            }
        }

        private static void GetInputDefinitions(CoherenceSync sync, List<InputDefinition> defs)
        {
            if (!sync.TryGetComponent(out CoherenceInput coherenceInput))
            {
                return;
            }

            var name = GetSchemaDefinitionName(sync);

            if (!string.IsNullOrEmpty(name) && coherenceInput.Fields != null)
            {
                name = NameMangler.MangleSchemaIdentifier(name);

                var members = new List<ComponentMemberDescription>();

                var fieldOffset = 0;

                foreach (var field in coherenceInput.Fields)
                {
                    if (!string.IsNullOrEmpty(field.name))
                    {
                        var schemaType = TypeUtils.GetSchemaTypeForInputType(field.type);
                        var cSharpType = TypeUtils.GetCSharpTypeForSchemaType(schemaType.ToString());
                        var fieldSize = TypeUtils.GetFieldOffsetForSchemaType(schemaType);

                        var cSharpTypeFullName = cSharpType.FullName;

                        if (cSharpTypeFullName.Contains("UnityEngine"))
                        {
                            cSharpTypeFullName = cSharpType.Name;
                        }

                        var member = new ComponentMemberDescription(
                            NameMangler.MangleSchemaIdentifier(field.name),
                            NameMangler.MangleCSharpIdentifier(field.name),
                            schemaType.ToString(),
                            cSharpTypeFullName, string.Empty, fieldOffset);

                        members.Add(member);
                        fieldOffset += fieldSize;
                    }
                }

                var inputDefinition = new InputDefinition(name, members, fieldOffset);
                defs.Add(inputDefinition);
            }
        }

        private static void GetArchetypeDefinitionsFromSync(CoherenceSync sync,
            List<ArchetypeDefinition> archetypeDefinitions)
        {
            var archetype = sync.Archetype;

            var lods = new List<ArchetypeLOD>();

            foreach (var boundComponent in archetype.BoundComponents)
            {
                var level = 0;

                if (!boundComponent.ShouldBeIncludedInArchetype())
                {
                    continue;
                }

                var component = boundComponent.Component;

                if (!component)
                {
                    continue;
                }

                var hasProvider = EditorCache.GetBindingProviderForComponent(component, out var provider);

                for (var lod = 0; lod < boundComponent.LodStepsActive; lod++)
                {
                    if (level >= lods.Count)
                    {
                        var distance = archetype.LODLevels[level].Distance;
                        lods.Add(new ArchetypeLOD(level, distance));
                    }

                    var fieldsWithOverrides = FieldsWithOverrides(sync, boundComponent, lod);

                    if (hasProvider)
                    {
                        if (provider.AssociateCoherenceComponentTypePerBinding)
                        {
                            foreach (var binding in boundComponent.Bindings)
                            {
                                if (binding == null)
                                {
                                    continue;
                                }

                                var bind = sync.Bindings.FirstOrDefault(b =>
                                    b != null && b.Name == binding.Name && b.unityComponent == component);

                                if (bind == null)
                                {
                                    continue;
                                }

                                var field = bind.archetypeData.GetLODstep(lod);

                                var schemaComponentName = bind.CoherenceComponentName;
                                if (!string.IsNullOrEmpty(schemaComponentName))
                                {
                                    var itemFields = new List<ArchetypeItemField>();
                                    var mangledFieldName = NameMangler.MangleSchemaIdentifier(binding.SchemaFieldName);
                                    var overrides = LODOverrides(binding, field);
                                    itemFields.Add(new ArchetypeItemField(mangledFieldName, overrides));
                                    var archetypeItem = new ArchetypeItem(schemaComponentName, itemFields, GetBakeConditional(component));
                                    lods[level].items.Add(archetypeItem);
                                }
                            }
                        }
                        else
                        {
                            var schemaComponentName = GetSchemaDefinitionName(sync, GetLocalIdSuffix(component));
                            var archetypeItem = new ArchetypeItem(schemaComponentName, fieldsWithOverrides, GetBakeConditional(component));
                            lods[level].items.Add(archetypeItem);
                        }
                    }
                    else
                    {
                        var schemaComponentName = GetSchemaDefinitionName(sync, GetLocalIdSuffix(component));
                        var archetypeItem = new ArchetypeItem(schemaComponentName, fieldsWithOverrides, GetBakeConditional(component));
                        lods[level].items.Add(archetypeItem);
                    }

                    level++;
                }
            }

            if (lods.Count > 1)
            {
                var highestLod = lods[0];

                for (var i = 1; i < lods.Count; i++)
                {
                    var currentLod = lods[i];

                    foreach (var item in highestLod.items)
                    {
                        var foundInCurrentLod = currentLod.items.Any(componentInCurrent =>
                            item.componentName.Equals(componentInCurrent.componentName));

                        if (!foundInCurrentLod)
                        {
                            currentLod.excludedComponentNames.Add(item.componentName);
                        }
                    }
                }
            }

            CheckComponentLimit(sync, lods);

            var archetypeDefinition = new ArchetypeDefinition(((ICoherenceSync)sync).ArchetypeName, lods);
            archetypeDefinitions.Add(archetypeDefinition);
        }

        private static void CheckComponentLimit(CoherenceSync sync, List<ArchetypeLOD> lods)
        {
            if (lods.Count > 0 && lods[0].items.Count > MaxArchetypeLodComponents)
            {
                throw new InvalidCoherenceSyncException(
                    $"Prefab {sync.gameObject.name} has surpassed the {MaxArchetypeLodComponents} " +
                    $"components limit. It has {lods[0].items.Count} bound components. This is usually due to syncing variables" +
                    " in more than 32 different MonoBehaviours in your prefab hierarchy." +
                    $" Please remove {lods[0].items.Count - MaxArchetypeLodComponents} or more bound components to be able to bake.");
            }
        }

        private static (SchemaDefinition schemaDefinition, EntitiesBakeData entitiesData) SaveSchemaAndJson()
        {
            var behaviourData = new List<SyncedBehaviour>();

            var componentDefinitions = new List<ComponentDefinition>();
            var commandDefinitions = new List<CommandDefinition>();
            var archetypeDefinitions = new List<ArchetypeDefinition>();
            var inputDefinitions = new List<InputDefinition>();

            var inputData = new Dictionary<string, string>();

            // Used for detecting name clashes
            // Maps from mangled component name to original name
            var originalPrefabNames = new Dictionary<string, string>();

            var minIds = LoadIdOffsets();

            var syncs = new List<CoherenceSync>();
            CoherenceSyncConfigRegistry.Instance.GetReferencedPrefabs(syncs);
            foreach (var sync in syncs)
            {
                if (sync.CoherenceSyncConfig != null && !sync.CoherenceSyncConfig.IncludeInSchema)
                {
                    continue;
                }

                sync.ValidateArchetype();
                var generatesArchetype = sync.Archetype.GeneratesArchetypeDefinition;
                var syncTheseComponents = new List<SyncedComponent>();
                var handleTheseCommands = new List<CommandDescription>();

                var orderedBindings = GetOrderedBindings(sync, out var invalidBindings);

                ThrowExceptionIfTooManyNetworkComponents(orderedBindings, sync);

                if (invalidBindings > 0)
                {
                    logger.Warning(Warning.EditorSchemaCreatorInvalidBindings,
                        $"Networked Prefab {sync.name} contains invalid bindings that are skipped during baking.",
                        ("sync", sync),
                        ("path", AssetDatabase.GetAssetPath(sync)));
                }

                foreach (var componentKv in orderedBindings)
                {
                    var component = componentKv.Key;

                    var commandOverloads = new Dictionary<string, int>();
                    var componentType = component.GetType();

                    var componentName = componentType.FullName;

                    var syncTheseMembers = new List<string>();
                    var memberNamesInUnityComponent = new List<string>();
                    var syncTheseCSharpTypes = new List<string>();
                    var bindingNames = new List<string>();
                    var bindingGuids = new List<string>();
                    var bindingClassNames = new List<string>();
                    var bitMasks = new List<string>();

                    var syncTheseGetters = new List<string>();
                    var syncTheseSetters = new List<string>();
                    var componentsPerBindingDict = new Dictionary<string, SyncedComponent>();

                    _ = EditorCache.GetBindingProviderForComponent(component, out var provider);

                    var createCachedProperty = provider.EmitMonoComponentReferenceOnBakedSyncScript;
                    var possibleOverride = provider.MonoComponentReferenceFieldNameOverride ??
                                           $"_{NameMangler.MangleSchemaIdentifier(componentName.ToLower())}";
                    var cachedUnityComponentReferenceName = possibleOverride + GetLocalIdSuffix(component);
                    var cachedUnityComponentReferenceType =
                        provider.MonoComponentReferenceTypeOverride ?? componentType;

                    int fieldOffset = 0;
                    foreach (var binding in componentKv.Value)
                    {
                        binding.Activate();

                        // command
                        if (binding is CommandBinding commandBinding)
                        {
                            var schemaCommandName = GetSchemaDefinitionName(sync, binding.guid);

                            SaveJsonInformationForMethodBinding(commandBinding, schemaCommandName, handleTheseCommands);
                            GatherSchemaDescriptionForMethodBinding(commandBinding, commandDefinitions, schemaCommandName);

                            continue;
                        }

                        SaveJsonInformationForVariableBinding(binding, provider, createCachedProperty,
                            cachedUnityComponentReferenceName,
                            cachedUnityComponentReferenceType, component, componentsPerBindingDict, syncTheseComponents,
                            syncTheseMembers, memberNamesInUnityComponent,
                            syncTheseGetters, syncTheseSetters, syncTheseCSharpTypes, bindingNames, bindingGuids,
                            bindingClassNames, bitMasks);

                        if (!provider.EmitSchemaComponentDefinition || !binding.EmitSchemaComponentDefinition)
                        {
                            continue;
                        }

                        var fieldSize = TypeUtils.GetFieldOffsetForSchemaType(
                            TypeUtils.GetSchemaType(binding.MonoAssemblyRuntimeType));

                        GatherSchemaDescriptionForVariableBinding(provider, componentType, fieldOffset, binding, sync,
                            originalPrefabNames, componentDefinitions);

                        fieldOffset += fieldSize;
                    }

                    if (provider.AssociateCoherenceComponentTypePerBinding)
                    {
                        continue;
                    }

                    if (syncTheseMembers.Count == 0)
                    {
                        continue;
                    }

                    var schemaComponentName = GetSchemaDefinitionName(sync, GetLocalIdSuffix(component));

                    var fieldMasks = (1 << bitMasks.Count) - 1;

                    var membersInfo = new List<ComponentMember>();

                    for (var i = 0; i < syncTheseMembers.Count; i++)
                    {
                        membersInfo.Add(new ComponentMember
                        {
                            MemberNameInComponentData = syncTheseMembers[i],
                            MemberNameInUnityComponent = memberNamesInUnityComponent[i],
                            BindingClassName = bindingClassNames[i],
                            BindingGuid = bindingGuids[i],
                            BindingName = bindingNames[i],
                            PropertyBitMask = bitMasks[i],
                            PropertyCSharpType = syncTheseCSharpTypes[i],
                            PropertyGetter = syncTheseGetters[i],
                            PropertySetter = syncTheseSetters[i],
                        });
                    }

                    var syncedComponent2 = new SyncedComponent(
                        schemaComponentName,
                        provider.EmitMonoComponentReferenceOnBakedSyncScript,
                        cachedUnityComponentReferenceName,
                        cachedUnityComponentReferenceType.FullName,
                        component.GetType().FullName,
                        false,
                        false,
                        membersInfo,
                        GetBakeConditional(component)
                    )
                    {
                        FieldMasks = fieldMasks,
                    };

                    syncTheseComponents.Add(syncedComponent2);
                }

                behaviourData.Add(new SyncedBehaviour(
                    BakedScriptName(sync),
                    sync.name,
                    sync.CoherenceSyncConfig.ID,
                    sync.IsGlobal,
                    syncTheseComponents.ToArray(),
                    handleTheseCommands.ToArray()));

                GetInputDefinitions(sync, inputDefinitions);

                if (generatesArchetype)
                {
                    GetArchetypeDefinitionsFromSync(sync, archetypeDefinitions);
                }

                if (!sync.TryGetComponent(out CoherenceInput _))
                {
                    continue;
                }

                var className = BakedScriptName(sync);
                // This name must correspond to the generated input definition
                var name = GetSchemaDefinitionName(sync);
                inputData.Add(className, name);
            }

            var validatedArchetypes = ValidateArchetypes(archetypeDefinitions, componentDefinitions);

            validatedArchetypes.Sort((x, y) =>
                string.Compare(x.name, y.name, StringComparison.Ordinal));
            componentDefinitions.Sort((x, y) =>
                string.Compare(x.name, y.name, StringComparison.Ordinal));
            commandDefinitions.Sort((x, y) =>
                string.Compare(x.name, y.name, StringComparison.Ordinal));
            inputDefinitions.Sort((x, y) =>
                string.Compare(x.name, y.name, StringComparison.Ordinal));

            var schemaDefinition = new SchemaDefinition
            {
                ArchetypeDefinitions = validatedArchetypes,
                CommandDefinitions = commandDefinitions,
                ComponentDefinitions = componentDefinitions,
                InputDefinitions = inputDefinitions,
            };

            SetDefinitionIds(minIds, schemaDefinition);

            var codeGeneratorData = new EntitiesBakeData
            {
                BehaviourData = behaviourData,
                InputData = inputData,
            };

            WriteSchemaToDisk(schemaDefinition);

            return (schemaDefinition, codeGeneratorData);
        }

        private static void SetDefinitionIds(IdOffsets minIds, SchemaDefinition schemaDefinition)
        {
            var componentId = minIds.ComponentId;
            var componentsDictionary = new Dictionary<string, ComponentDefinition>();

            foreach (var component in schemaDefinition.ComponentDefinitions)
            {
                componentId++;
                component.id = componentId;
                componentsDictionary[component.name] = component;
            }

            IndexRsAndToolkitComponentDefinitions(componentsDictionary);

            var commandId = minIds.CommandId;

            foreach (var command in schemaDefinition.CommandDefinitions)
            {
                commandId++;
                command.id = commandId;
            }

            var archetypeId = minIds.ArchetypeId;

            foreach (var archetype in schemaDefinition.ArchetypeDefinitions)
            {
                archetypeId++;
                archetype.id = archetypeId;

                foreach (var lod in archetype.lods)
                {
                    foreach (var componentOverride in lod.items)
                    {
                        componentId++;
                        componentOverride.id = componentId;

                        if (!componentsDictionary.TryGetValue(componentOverride.componentName, out var componentDef))
                        {
                            throw new InvalidDataException(
                                $"No Component Definition found for {componentOverride.componentName}");
                        }

                        componentOverride.baseComponentId = componentDef.id;
                    }
                }
            }

            var inputId = minIds.InputId;

            foreach (var input in schemaDefinition.InputDefinitions)
            {
                inputId++;
                input.id = inputId;
            }
        }

        private static void IndexRsAndToolkitComponentDefinitions(
            Dictionary<string, ComponentDefinition> componentsDictionary)
        {
            var engineSchema = AssetDatabase.LoadAssetAtPath<SchemaAsset>(Paths.rsSchemaPath);

            foreach (var component in engineSchema.SchemaDefinition.ComponentDefinitions)
            {
                componentsDictionary[component.name] = component;
            }

            var toolkitSchema = AssetDatabase.LoadAssetAtPath<SchemaAsset>(Paths.toolkitSchemaPath);

            foreach (var component in toolkitSchema.SchemaDefinition.ComponentDefinitions)
            {
                componentsDictionary[component.name] = component;
            }
        }

        private static IdOffsets LoadIdOffsets()
        {
            var toolkitSchema = AssetDatabase.LoadAssetAtPath<SchemaAsset>(Paths.toolkitSchemaPath);
            var rsSchema = AssetDatabase.LoadAssetAtPath<SchemaAsset>(Paths.rsSchemaPath);

            var minIds = new IdOffsets();

            GetIdOffsetsFromSchema(toolkitSchema, minIds);
            GetIdOffsetsFromSchema(rsSchema, minIds);

            return minIds;
        }

        private static void GetIdOffsetsFromSchema(SchemaAsset toolkitSchema, IdOffsets minIds)
        {
            foreach (var component in toolkitSchema.SchemaDefinition.ComponentDefinitions)
            {
                minIds.ComponentId = Math.Max(component.id, minIds.ComponentId);
            }

            foreach (var command in toolkitSchema.SchemaDefinition.CommandDefinitions)
            {
                minIds.CommandId = Math.Max(command.id, minIds.CommandId);
            }

            foreach (var archetype in toolkitSchema.SchemaDefinition.ArchetypeDefinitions)
            {
                minIds.ArchetypeId = Math.Max(archetype.id, minIds.ArchetypeId);
            }

            foreach (var input in toolkitSchema.SchemaDefinition.InputDefinitions)
            {
                minIds.InputId = Math.Max(input.id, minIds.InputId);
            }
        }

        private static string ResolveSetter(string setter, string cSharpType, string propertyType, bool overrideSetter)
        {
            if (overrideSetter)
            {
                return setter.Replace("@", "value");
            }

            if (setter.Contains("@"))
            {
                return $"CastedUnityComponent.{setter.Replace("@", "value")}";
            }

            var toUnityObject = cSharpType switch
            {
                "UnityEngine.GameObject" => "coherenceSync.CoherenceBridge.EntityIdToGameObject",
                "UnityEngine.Transform" => "coherenceSync.CoherenceBridge.EntityIdToTransform",
                "Coherence.Toolkit.CoherenceSync" => "coherenceSync.CoherenceBridge.EntityIdToCoherenceSync",
                "UnityEngine.RectTransform" => "coherenceSync.CoherenceBridge.EntityIdToRectTransform",
                var _ => $"{(!string.IsNullOrEmpty(cSharpType) ? $"({cSharpType})" : string.Empty)}",
            };

            return propertyType.Equals("Coherence.Toolkit.CoherenceSync")
                ? $"coherenceSync.{setter} = {toUnityObject}(value)"
                : $"CastedUnityComponent.{setter} = {toUnityObject}(value)";
        }

        private static string ResolveGetter(string getter, string cSharpType, string propertyType, bool overrideGetter)
        {
            if (overrideGetter)
            {
                return getter;
            }

            var toUnityObject = string.Empty;

            switch (cSharpType)
            {
                case "UnityEngine.GameObject":
                case "UnityEngine.Transform":
                case "Coherence.Toolkit.CoherenceSync":
                case "UnityEngine.RectTransform":
                    toUnityObject = "coherenceSync.CoherenceBridge.UnityObjectToEntityId";
                    break;
                default:
                    if (!string.IsNullOrEmpty(cSharpType))
                    {
                        toUnityObject = "(" + cSharpType + ")";
                    }

                    break;
            }

            if (propertyType.Equals("Coherence.Toolkit.CoherenceSync"))
            {
                return $"return {toUnityObject}(coherenceSync.{getter})";
            }

            return $"return {toUnityObject}(CastedUnityComponent.{getter})";
        }

        private static void ThrowExceptionIfTooManyNetworkComponents(
            Dictionary<Component, List<Binding>> orderedBindings, CoherenceSync sync)
        {
            // Initial network components are the amount of unity components bound + 1 for the Asset Id component which is always added to every entity.
            var networkComponents = orderedBindings.Count + 1;

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

            if (networkComponents > BakeUtil.MaxUniqueComponentsBound)
            {
                throw new InvalidOperationException(
                    $"{sync.name} will create {networkComponents} Network Components at runtime, this is limited to {BakeUtil.MaxUniqueComponentsBound}, surpassing this limit will create errors at runtime." +
                    "Check the CoherenceSync inspector and Configure window for more details.");
            }
        }

        private static void WriteSchemaToDisk(SchemaDefinition schemaDefinition)
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(Paths.gatherSchemaPath));

            var schemaWriter = new StreamWriter(Paths.gatherSchemaPath);
            var schemaCode = CreateSchema(
                schemaDefinition.ComponentDefinitions,
                schemaDefinition.CommandDefinitions,
                schemaDefinition.ArchetypeDefinitions,
                schemaDefinition.InputDefinitions);
            schemaWriter.Write(schemaCode);
            schemaWriter.Close();
            AssetUtils.ImportAsset(Paths.gatherSchemaPath);

            Schemas.InvalidateSchemaCache();
        }

        private static void SaveBundledSchema()
        {
            try
            {
                using (var writer = new StreamWriter(Paths.combinedSchemaPath))
                {
                    writer.Write(Schemas.GetCombinedSchemaContents());
                }
                AssetUtils.ImportAsset(Paths.combinedSchemaPath);
            }
            catch (Exception e)
            {
                logger.Error(Error.EditorSchemaCreatorSaveException, ("exception", e));
            }
        }

        private static void SaveJsonInformationForVariableBinding(Binding binding, DescriptorProvider provider,
            bool createCachedProperty, string cachedUnityComponentReferenceName, Type cachedUnityComponentReferenceType,
            Component component, Dictionary<string, SyncedComponent> syncComponentsDict,
            List<SyncedComponent> syncTheseComponents, List<string> syncTheseMembers,
            List<string> memberNamesInUnityComponent,
            List<string> syncTheseGetters, List<string> syncTheseSetters,
            List<string> syncTheseCSharpTypes, List<string> bindingNames, List<string> bindingGuids,
            List<string> bindingClassNames, List<string> bitMasks)
        {
            var getter = GetValueOrDefault(binding.BakedSyncScriptGetter, binding.Name);
            var setter = GetValueOrDefault(binding.BakedSyncScriptSetter, binding.Name);
            var csType = GetValueOrDefault(binding.BakedSyncScriptCSharpType, string.Empty);

            getter = ResolveGetter(getter, csType, cachedUnityComponentReferenceType.FullName, binding.OverrideGetter);
            setter = ResolveSetter(setter, csType, cachedUnityComponentReferenceType.FullName, binding.OverrideSetter);

            if (provider.AssociateCoherenceComponentTypePerBinding)
            {
                if (!syncComponentsDict.TryGetValue(binding.CoherenceComponentName, out var syncedComponent))
                {
                    syncedComponent = new SyncedComponent(
                        binding.CoherenceComponentName,
                        createCachedProperty,
                        cachedUnityComponentReferenceName,
                        cachedUnityComponentReferenceType.FullName,
                        component.GetType().FullName,
                        binding.OverrideSetter,
                        binding.OverrideGetter,
                        new List<ComponentMember>(),
                        GetBakeConditional(component)
                    );

                    syncTheseComponents.Add(syncedComponent);
                    syncComponentsDict.Add(binding.CoherenceComponentName, syncedComponent);
                }

                var bitMask = 1 << syncedComponent.MembersInfo.Count;

                syncedComponent.MembersInfo.Add(new ComponentMember
                {
                    MemberNameInComponentData = binding.MemberNameInComponentData,
                    MemberNameInUnityComponent = binding.MemberNameInUnityComponent,
                    BindingClassName = binding.GetType().Name,
                    BindingGuid = binding.guid,
                    BindingName = binding.Name,
                    PropertyBitMask = TypeUtils.GetStringifiedBitMask(bitMask),
                    PropertyCSharpType = csType,
                    PropertyGetter = getter,
                    PropertySetter = setter,
                });

                syncedComponent.FieldMasks = (1 << syncedComponent.MembersInfo.Count) - 1;

                // if 1 binding = 1 component, make sure we don't create cached property duplicates
                if (createCachedProperty)
                {
                    createCachedProperty = false;
                }
            }
            else
            {
                var bitMask = 1 << syncTheseMembers.Count;

                var componentDataMemberName = NameMangler.MangleSchemaIdentifier(binding.MemberNameInComponentData);
                syncTheseMembers.Add(componentDataMemberName);
                memberNamesInUnityComponent.Add(binding.MemberNameInUnityComponent);
                syncTheseGetters.Add(getter);
                syncTheseSetters.Add(setter);
                syncTheseCSharpTypes.Add(csType);
                bindingNames.Add(binding.Name);
                bindingGuids.Add(binding.guid);
                bindingClassNames.Add(binding.GetType().Name);
                bitMasks.Add(TypeUtils.GetStringifiedBitMask(bitMask));
            }
        }

        private static void SaveJsonInformationForMethodBinding(CommandBinding commandBinding,
            string overloadedCommandName,
            List<CommandDescription> handleTheseCommands)
        {
            var parameters = new List<CommandParameterInfo>();

            foreach (var parameterInfo in commandBinding.GetMethodInfo().GetParameters())
            {
                var paramInfo = new CommandParameterInfo
                {
                    Name = parameterInfo.Name,
                    Type = parameterInfo.ParameterType.FullName,
                    BakedType = TypeUtils.GetSchemaType(parameterInfo.ParameterType) == SchemaType.Entity
                        ? SchemaType.Entity.ToString()
                        : parameterInfo.ParameterType.FullName,
                };
                parameters.Add(paramInfo);
            }

            var command = new CommandDescription(
                overloadedCommandName,
                commandBinding.GetMethodInfo().Name,
                commandBinding.GetMethodInfo().DeclaringType.FullName,
                commandBinding.Name,
                commandBinding.guid,
                commandBinding.routing.ToString(),
                parameters,
                GetBakeConditional(commandBinding.unityComponent));

            handleTheseCommands.Add(command);
        }

        private static void GatherSchemaDescriptionForVariableBinding(DescriptorProvider provider, Type componentType, int fieldOffset,
            Binding binding, CoherenceSync sync, Dictionary<string, string> originalPrefabNames,
            List<ComponentDefinition> componentDefinitions)
        {
            string schemaComponentName;

            if (provider.AssociateCoherenceComponentTypePerBinding)
            {
                schemaComponentName = binding.CoherenceComponentName;
            }
            else
            {
                schemaComponentName = GetSchemaDefinitionName(sync, GetLocalIdSuffix(binding.unityComponent));

                if (DetectDuplicateMangledSchemaComponentName(originalPrefabNames, sync.name, schemaComponentName))
                {
                    return;
                }
            }

            if (!TryGetComponent(schemaComponentName, componentDefinitions, out var def))
            {
                def = new ComponentDefinition(schemaComponentName, GetBakeConditional(binding.unityComponent));
                componentDefinitions.Add(def);
            }

            var schemaType = TypeUtils.GetSchemaType(binding.MonoAssemblyRuntimeType);

            var baseLODStep = binding.BindingArchetypeData.GetLODstep(0);
            var overrides = LODOverrides(binding, baseLODStep);

            var bitMask = 1 << def.members.Count;

            var cSharpTypeFullName = binding.MonoAssemblyRuntimeType.FullName;

            if (cSharpTypeFullName.Contains("UnityEngine"))
            {
                cSharpTypeFullName = binding.MonoAssemblyRuntimeType.Name;
            }

            if (schemaType == SchemaType.Entity)
            {
                overrides["client-type"] = cSharpTypeFullName;
                cSharpTypeFullName = "Entity";
            }

            def.members.Add(new ComponentMemberDescription(
                NameMangler.MangleSchemaIdentifier(binding.Name),
                NameMangler.MangleCSharpIdentifier(binding.Name),
                schemaType.ToString(),
                cSharpTypeFullName,
                TypeUtils.GetStringifiedBitMask(bitMask),
                fieldOffset,
                overrides));

            def.bitMasks = TypeUtils.GetStringifiedBitMask((1 << def.members.Count) - 1);
            def.totalSize += TypeUtils.GetFieldOffsetForSchemaType(TypeUtils.GetSchemaType(binding.MonoAssemblyRuntimeType));
        }

        private static bool TryGetComponent(string name, List<ComponentDefinition> components,
            out ComponentDefinition comp)
        {
            comp = null;
            foreach (var component in components)
            {
                if (component.name.Equals(name))
                {
                    comp = component;
                    return true;
                }
            }

            return false;
        }

        private static void GatherSchemaDescriptionForMethodBinding(CommandBinding commandBinding,
            List<CommandDefinition> commandDefinitions,
            string overloadedSchemaCommandName)
        {
            var commandArgs = new List<ComponentMemberDescription>();
            var fieldOffset = 0;
            foreach (var parameterInfo in commandBinding.GetMethodInfo().GetParameters())
            {
                var schemaType = TypeUtils.GetSchemaType(parameterInfo.ParameterType);
                var fieldSize = TypeUtils.GetFieldOffsetForSchemaType(TypeUtils.GetSchemaType(parameterInfo.ParameterType));
                var overrides = CommandOverrides(schemaType);

                var cSharpTypeFullName = parameterInfo.ParameterType.FullName;

                if (cSharpTypeFullName.Contains("UnityEngine"))
                {
                    cSharpTypeFullName = parameterInfo.ParameterType.Name;
                }

                if (schemaType == SchemaType.Entity)
                {
                    overrides["client-type"] = cSharpTypeFullName;
                    cSharpTypeFullName = "Entity";
                }

                var member = new ComponentMemberDescription(
                    NameMangler.MangleSchemaIdentifier(parameterInfo.Name),
                    NameMangler.MangleCSharpIdentifier(parameterInfo.Name),
                    schemaType.ToString(),
                    cSharpTypeFullName,
                    string.Empty,
                    fieldOffset,
                    overrides);

                commandArgs.Add(member);
                fieldOffset += fieldSize;
            }

            commandDefinitions.Add(new CommandDefinition(overloadedSchemaCommandName, commandArgs,
                commandBinding.routing, fieldOffset, GetBakeConditional(commandBinding.unityComponent)));
        }

        private static Dictionary<Component, List<Binding>> GetOrderedBindings(CoherenceSync sync,
            out int invalidBindings)
        {
            invalidBindings = 0;

            var orderedBindings = new Dictionary<Component, List<Binding>>();

            for (var index = 0; index < sync.Bindings.Count; index++)
            {
                var binding = sync.Bindings[index];
                if (!CoherenceSyncUtils.IsBindingValid(sync, index, out _))
                {
                    invalidBindings++;
                    continue;
                }

                if (!orderedBindings.TryGetValue(binding.unityComponent, out var bindings))
                {
                    bindings = new List<Binding>();
                    orderedBindings.Add(binding.unityComponent, bindings);
                }

                bindings.Add(binding);
            }

            return orderedBindings;
        }

        // Would be great if this was derived from the schema but that's a chicken & egg thing,
        // so hard-coding these here.
        private static readonly string[] builtInComponents =
        {
            "WorldPosition",
            "WorldOrientation",
            "GenericScale",
        };

        private static List<ArchetypeDefinition> ValidateArchetypes(List<ArchetypeDefinition> archetypes,
            List<ComponentDefinition> components)
        {
            var validated = new List<ArchetypeDefinition>();

            foreach (var archetype in archetypes)
            {
                if (archetype.lods == null || archetype.lods.Count == 0)
                {
                    logger.Warning(Warning.EditorSchemaCreatorNoLODs,
                        $"Couldn't bake LODs for {archetype.name}: found 0 LODs.",
                        ("sync", archetype.name));
                    continue;
                }

                var lodsValidated = true;
                foreach (var lod in archetype.lods)
                {
                    if (lod.items == null || lod.items.Count == 0)
                    {
                        lodsValidated = false;
                        logger.Warning(Warning.EditorSchemaCreatorNoComponents,
                            $"Couldn't bake LODs for {archetype.name}: LOD {lod.level} has no components.",
                            ("sync", archetype.name));
                    }
                    else
                    {
                        var componentsValidated = true;
                        foreach (var item in lod.items)
                        {
                            if (!ContainsComponent(item.componentName, components) &&
                                !builtInComponents.Contains(item.componentName))
                            {
                                componentsValidated = false;
                                logger.Warning(Warning.EditorSchemaCreatorInvalidComponents,
                                    $"Couldn't bake LODs for {archetype.name}: component {item.componentName} in LOD {lod.level} is invalid. Not a built-in component or has no binding.",
                                    ("sync", archetype.name));
                            }
                        }

                        if (!componentsValidated)
                        {
                            lodsValidated = false;
                        }
                    }
                }

                if (lodsValidated)
                {
                    validated.Add(archetype);
                }
            }

            return validated;
        }

        private static bool ContainsComponent(string name, List<ComponentDefinition> components)
        {
            foreach (var component in components)
            {
                if (component.name.Equals(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     All components with the same name must map to the same prefab.
        /// </summary>
        /// <returns>
        ///     `true` if a duplicate was found.
        /// </returns>
        private static bool DetectDuplicateMangledSchemaComponentName(Dictionary<string, string> originalPrefabNames,
            string prefabName, string componentName)
        {
            if (originalPrefabNames.TryGetValue(componentName, out var originalPrefabName))
            {
                if (prefabName != originalPrefabName)
                {
                    logger.Warning(Warning.EditorSchemaCreatorDuplicateNames,
                        $"Duplicate names generated by prefabs '{prefabName}' and '{originalPrefabName}'");
                    return true;
                }
            }
            else
            {
                originalPrefabNames[componentName] = prefabName;
            }

            return false;
        }

        private static string OverloadedCommandName(Dictionary<string, int> commandOverloads, string commandName)
        {
            if (commandOverloads.TryGetValue(commandName, out var overloads))
            {
                commandOverloads[commandName] = overloads + 1;
                return $"{commandName}_overload{overloads}";
            }

            commandOverloads[commandName] = 1;
            return commandName;
        }

        private static string GetSyncName(CoherenceSync sync)
        {
            return CoherenceSyncConfigUtils.TryGetFromAsset(sync, out var config)
                ? config.ID
                : string.Empty;
        }

        private static string GetSchemaDefinitionName(CoherenceSync sync, string identifier = null)
        {
            // generated schema definition names preceded with an underscore
            var s = "_" + GetSyncName(sync);

            if (!string.IsNullOrEmpty(identifier))
            {
                s += $"_{identifier}";
            }

            return NameMangler.MangleSchemaIdentifier(s);
        }

        private static string GetLocalIdSuffix(Object unityComponent)
        {
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(unityComponent, out var _, out long localId)
                ? ((ulong)localId).ToString()
                : string.Empty;
        }

        private static DictionaryOfStringString CommandOverrides(SchemaType schemaType)
        {
            var overrides = new DictionaryOfStringString();
            if (BindingArchetypeData.IsBitsBased(schemaType))
            {
                overrides["bits"] = "32";
            }
            else if (BindingArchetypeData.IsFloatBased(schemaType))
            {
                overrides["compression"] = FloatCompression.None.ToString();
            }

            if (schemaType == SchemaType.Color)
            {
                overrides["compression"] = FloatCompression.FixedPoint.ToString();
                overrides["range-min"] = "0";
                overrides["range-max"] = "1";
                overrides["precision"] = ArchetypeMath.GetPrecisionByBitsAndRange(32, 1)
                    .ToString(CultureInfo.InvariantCulture);
            }

            return overrides;
        }

        private static DictionaryOfStringString LODOverrides(
            Binding binding,
            BindingLODStepData lodStepData)
        {
            var archetypeData = binding.archetypeData;

            var overrides = new DictionaryOfStringString();

            if (BindingArchetypeData.IsBitsBased(lodStepData.SchemaType))
            {
                if (lodStepData.Bits < 1 || lodStepData.Bits > 32)
                {
                    overrides["bits"] = "32";
                }
                else
                {
                    overrides["bits"] = lodStepData.Bits.ToString();
                }
            }
            else if (lodStepData.Bits > 0)
            {
                overrides["bits"] = lodStepData.Bits.ToString();
            }

            var hasRange = archetypeData.MinRange != archetypeData.MaxRange;
            if (hasRange)
            {
                overrides["range-min"] = archetypeData.MinRange.ToString();
                overrides["range-max"] = archetypeData.MaxRange.ToString();
            }

            if (lodStepData.IsFloatType)
            {
                overrides["compression"] = lodStepData.FloatCompression.ToString();

                if (lodStepData.FloatCompression == FloatCompression.FixedPoint)
                {
                    overrides["precision"] = lodStepData.Precision.ToString(CultureInfo.InvariantCulture);
                    if (!hasRange)
                    {
                        (var min, var max) = archetypeData.GetRangeByLODs();
                        overrides["range-min"] = min.ToString();
                        overrides["range-max"] = max.ToString();
                    }
                }

                if (lodStepData.FloatCompression == FloatCompression.None)
                {
                    overrides.Remove("bits");
                }
            }

            if (binding.interpolationSettings != null && !binding.interpolationSettings.IsInterpolationNone)
            {
                overrides["sim-frames"] = "true";
            }

            AddColorOverrides(lodStepData, overrides);

            return overrides;
        }

        private static void AddColorOverrides(BindingLODStepData lodStepData, DictionaryOfStringString overrides)
        {
            if (lodStepData.SchemaType == SchemaType.Color)
            {
                overrides["compression"] = FloatCompression.FixedPoint.ToString();
                overrides["range-min"] = "0";
                overrides["range-max"] = "1";
                overrides["precision"] = lodStepData.Precision.ToString(CultureInfo.InvariantCulture);
            }
        }

        // Data from Sync
        private static List<ArchetypeItemField> FieldsWithOverrides(CoherenceSync sync,
            ArchetypeComponent boundComponent, int lodstep)
        {
            var fields = new List<ArchetypeItemField>();
            var bindings = boundComponent.GetAllBindingsOnSync(sync);

            foreach (var binding in bindings)
            {
                var lodStepData = binding.BindingArchetypeData.GetLODstep(lodstep);
                if (lodStepData != null)
                {
                    var overrides = LODOverrides(binding, lodStepData);
                    var archetypeItemField = new ArchetypeItemField(binding.Name, overrides);

                    if (archetypeItemField.overrides.Count > 0)
                    {
                        fields.Add(archetypeItemField);
                    }
                }
            }

            return fields;
        }

        private static string CreateSchema(IEnumerable<ComponentDefinition> components,
            IEnumerable<CommandDefinition> commands,
            IEnumerable<ArchetypeDefinition> archetypes,
            IEnumerable<InputDefinition> inputs)
        {
            var writer = new StringWriter();

            foreach (var component in components)
            {
                writer.Write($"component {component.name} [id \"{component.id}\"]\n");
                foreach (var member in component.members)
                {
                    writer.Write($"  {member.variableName} {member.typeName} ");
                    WriteMeta(writer, member.overrides);
                    writer.Write("\n");
                }

                writer.Write("\n");
            }

            foreach (var command in commands)
            {
                writer.Write($"command {command.name} [id \"{command.id}\", routing \"{command.routing}\"]\n");
                foreach (var member in command.members)
                {
                    writer.Write($"  {member.variableName} {member.typeName} ");
                    WriteMeta(writer, member.overrides);
                    writer.Write("\n");
                }

                writer.Write("\n");
            }

            foreach (var archetype in archetypes)
            {
                writer.Write($"archetype {NameMangler.MangleSchemaIdentifier(archetype.name)} [id \"{archetype.id}\"]\n");
                foreach (var lod in archetype.lods)
                {
                    writer.Write($"  lod {lod.level}");
                    if (lod.level > 0)
                    {
                        writer.Write($" [distance \"{lod.distance}\"]");
                    }

                    writer.Write("\n");
                    foreach (var item in lod.items)
                    {
                        writer.Write(
                            $"    {item.componentName} [id \"{item.id}\", base-id \"{item.baseComponentId}\"]");
                        writer.Write("\n");
                        foreach (var field in item.fields)
                        {
                            writer.Write($"      archetype_field {field.fieldName} ");
                            WriteMeta(writer, field.overrides);
                            writer.Write("\n");
                        }
                    }
                }

                writer.Write("\n");
            }

            foreach (var input in inputs)
            {
                writer.Write($"input {input.name} [id \"{input.id}\"]\n");
                foreach (var field in input.members)
                {
                    writer.Write($"  {field.variableName}");
                    switch (field.typeName)
                    {
                        case "Vector2":
                            writer.Write($" Vector2 [compression \"{FloatCompression.None}\"]\n");
                            break;
                        case "Vector3":
                            writer.Write($" Vector3 [compression \"{FloatCompression.None}\"]\n");
                            break;
                        case "Quaternion":
                            writer.Write(" Quaternion [bits \"32\"]\n");
                            break;
                        case "Int":
                            writer.Write(
                                $" Int [bits \"32\", range-min \"{int.MinValue}\", range-max \"{int.MaxValue}\"]\n");
                            break;
                        case "Bool":
                            writer.Write(" Bool\n");
                            break;
                        case "Float":
                            writer.Write($" Float [compression \"{FloatCompression.None}\"]\n");
                            break;
                        case "String":
                            writer.Write(" String\n");
                            break;
                    }
                }

                writer.Write("\n");
            }

            writer.Close();
            return writer.ToString();
        }

        private static void WriteMeta(StringWriter writer, Dictionary<string, string> overrides)
        {
            if (overrides == null)
            {
                return;
            }

            if (overrides.Count > 0)
            {
                writer.Write("[");
                var i = 0;
                foreach (var pair in overrides)
                {
                    writer.Write($"{pair.Key} \"{pair.Value}\"");
                    if (i < overrides.Count - 1)
                    {
                        writer.Write(", ");
                    }

                    i++;
                }

                writer.Write("]");
            }
        }

        public static string BakedScriptName(CoherenceSync sync)
        {
            return NameMangler.MangleCSharpIdentifier("CoherenceSync_" + GetSyncName(sync));
        }

        private static string GetBakeConditional(Component component)
        {
            var conditional = component.GetType().GetCustomAttribute<BakeConditionalAttribute>();
            return conditional?.Condition ?? string.Empty;
        }
    }
}
