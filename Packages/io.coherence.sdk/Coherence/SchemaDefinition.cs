// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence
{
    using System;
    using System.Collections.Generic;

#if !UNITY
    using DictionaryOfStringString = Dictionary<string, string>;
#endif

    public class IdOffsets
    {
        public int ComponentId = -1;
        public int CommandId = -1;
        public int ArchetypeId = -1;
        public int InputId = -1;
    }

    [Serializable]
    public class SchemaDefinition
    {
        public string SchemaId;
        public List<ComponentDefinition> ComponentDefinitions = new();
        public List<CommandDefinition> CommandDefinitions = new();
        public List<ArchetypeDefinition> ArchetypeDefinitions = new();
        public List<InputDefinition> InputDefinitions = new();
    }

    // Used for schema generation -- corresponds to an coherence ECS component.
    [Serializable]
    public class ComponentDefinition : BaseDefinition
    {
        public string bitMasks;
        public List<ComponentMemberDescription> members;
        public DictionaryOfStringString overrides = new();
        public int totalSize;

        // Only relevant for archetype generated components
        public string baseComponentName;
        public bool generatedByArchetype;
        public string bakeConditional;

        public ComponentDefinition(string name, string bakeConditional = "") : base(name)
        {
            bitMasks = string.Empty;
            members = new List<ComponentMemberDescription>();
            this.bakeConditional = bakeConditional;
        }
    }

    [Serializable]
    public abstract class BaseDefinition
    {
        public int id;
        public string name;

        protected BaseDefinition(string name)
        {
            this.name = name;
        }
    }

    // Used for schema generation -- corresponds to an coherence ECS command.
    [Serializable]
    public class CommandDefinition : BaseDefinition
    {
        public List<ComponentMemberDescription> members;
        public MessageTarget routing;
        public int totalSize;
        public string bakeConditional;

        public CommandDefinition(string name, string bakeConditional = "")
            : this(name, new List<ComponentMemberDescription>(), MessageTarget.All, 0, bakeConditional)
        {
        }

        public CommandDefinition(string name, List<ComponentMemberDescription> members, MessageTarget routing, int totalSize, string bakeConditional) : base(name)
        {
            this.members = members;
            this.routing = routing;
            this.totalSize = totalSize;
            this.bakeConditional = bakeConditional;
        }
    }

    // Used for schema generation -- corresponds to an coherence ECS component member.
    [Serializable]
    public class ComponentMemberDescription
    {
        public string variableName;
        public string cSharpVariableName;
        public string typeName;
        public string cSharpTypeName;
        public string bitMask;
        public int fieldOffset;
        public DictionaryOfStringString overrides;

        public ComponentMemberDescription(string variableName, string cSharpVariableName, string typeName, string cSharpTypeName, string bitMask,
            int fieldOffset, DictionaryOfStringString overrides = null)
        {
            this.variableName = variableName;
            this.cSharpVariableName = cSharpVariableName;
            this.typeName = typeName;
            this.cSharpTypeName = cSharpTypeName;
            this.bitMask = bitMask;
            this.fieldOffset = fieldOffset;
            this.overrides = overrides;
        }

        public ComponentMemberDescription(ComponentMemberDescription other)
        {
            this.variableName = other.variableName;
            this.typeName = other.typeName;
            this.cSharpTypeName = other.cSharpTypeName;
            this.bitMask = other.bitMask;
            this.fieldOffset = other.fieldOffset;
            this.overrides = new DictionaryOfStringString();

            foreach (var overr in other.overrides)
            {
                overrides.Add(overr.Key, overr.Value);
            }
        }
    }

    [Serializable]
    public class ArchetypeDefinition : BaseDefinition
    {
        public List<ArchetypeLOD> lods;

        public ArchetypeDefinition(string name, List<ArchetypeLOD> lods) : base(name)
        {
            this.lods = lods;
        }
    }

    [Serializable]
    public class ArchetypeLOD
    {
        public int level;
        public List<ArchetypeItem> items;
        public float distance;
        public List<string> excludedComponentNames;

        public ArchetypeLOD(int level, float distance)
        {
            this.level = level;
            this.distance = distance;
            items = new List<ArchetypeItem>();
            excludedComponentNames = new List<string>();
        }
    }

    [Serializable]
    public class ArchetypeItem
    {
        public int id;
        public int baseComponentId;
        public string componentName;
        public string bakeConditional;
        public List<ArchetypeItemField> fields;

        public ArchetypeItem(string componentName, List<ArchetypeItemField> fields, string bakeConditional = "")
        {
            this.componentName = componentName;
            this.fields = fields;
            this.bakeConditional = bakeConditional;
        }
    }

    [Serializable]
    public class ArchetypeItemField
    {
        public string fieldName;
        public DictionaryOfStringString overrides;

        public ArchetypeItemField(string fieldName, DictionaryOfStringString overrides)
        {
            this.fieldName = fieldName;
            this.overrides = overrides;
        }
    }

    [Serializable]
    public class InputDefinition : BaseDefinition
    {
        public List<ComponentMemberDescription> members;
        public int totalSize;

        public InputDefinition(string name, List<ComponentMemberDescription> members, int totalSize) : base(name)
        {
            this.members = members;
            this.totalSize = totalSize;
        }
    }

    public struct EntitiesBakeData
    {
        public Dictionary<string, string> InputData;
        public List<SyncedBehaviour> BehaviourData;
    }

    // Used for json generation -- corresponds to one CoherenceSyncNAME being generated.
    // Note: The member names have some stuttering to make the json readable on its own
    public struct SyncedBehaviour
    {
        public string BehaviourName;
        public string UnmangledBehaviourName;
        public string AssetId;
        public bool IsGlobal;
        public SyncedComponent[] Components;
        public CommandDescription[] Commands;

        public SyncedBehaviour(string name,
            string unmangledName,
            string assetId,
            bool isGlobal,
            SyncedComponent[] components,
            CommandDescription[] commands)
        {
            BehaviourName = name;
            UnmangledBehaviourName = unmangledName;
            AssetId = assetId;
            IsGlobal = isGlobal;
            Components = components;
            Commands = commands;
        }
    }

    public class ComponentMember
    {
        public string MemberNameInComponentData;
        public string MemberNameInUnityComponent;
        public string BindingName;
        public string BindingGuid;
        public string BindingClassName;
        public string PropertyGetter;
        public string PropertySetter;
        public string PropertyCSharpType;
        public string PropertyBitMask;
    }

    // Used for json generation
    // Note: The member names have some stuttering to make the json readable on its own
    public class SyncedComponent
    {
        public string
            ComponentName; // Name of the ECS component that the sync script will sync the MonoBehaviour:s data with

        public bool NeedCachedProperty; // Will generate a _componentName reference in the sync script
        public string Property; // Name of the property to access the MonoBehaviour via, e.g. 'transform'
        public string PropertyType; // Type of the property to access the MonoBehaviour via, e.g. 'transform'
        public string UnityComponentType; // Type of the Unity component sync'd
        public bool OverrideSetter;
        public bool OverrideGetter;
        public int FieldMasks;
        public string FieldMasksString => Convert.ToString(FieldMasks, 2).PadLeft(32, '0');
        public List<ComponentMember> MembersInfo;
        public string BakeConditional; // Inserts #if {BakeConditional} into baked code

        public SyncedComponent(string name, bool needsInitializer, string property, string propertyType,
            string unityComponentType, bool overrideSetter, bool overrideGetter, List<ComponentMember> membersInfo,
            string bakeConditional)
        {
            ComponentName = name;
            NeedCachedProperty = needsInitializer;
            Property = property;
            PropertyType = propertyType;
            UnityComponentType = unityComponentType;
            OverrideSetter = overrideSetter;
            OverrideGetter = overrideGetter;
            MembersInfo = membersInfo;
            BakeConditional = bakeConditional;
        }
    }

    public class CommandDescription
    {
        public string CommandName;
        public string MethodName;
        public string MethodDeclaringClass;
        public string BindingName;
        public string BindingGuid;
        public string Routing;
        public List<CommandParameterInfo> ParametersInfo;
        public string BakeConditional;

        public CommandDescription(string name, string methodName, string declaringClass, string bindingName,
            string bindingGuid, string routing, List<CommandParameterInfo> parameters, string bakeConditional)

        {
            CommandName = name;
            MethodName = methodName;
            MethodDeclaringClass = declaringClass;
            BindingName = bindingName;
            BindingGuid = bindingGuid;
            Routing = routing;
            ParametersInfo = parameters;
            BakeConditional = bakeConditional;
        }
    }

    public class CommandParameterInfo
    {
        public string Name;
        public string BakedType;
        public string Type;
    }
}
