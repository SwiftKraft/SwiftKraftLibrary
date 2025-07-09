// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Bindings
{
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    [Serializable]
    [DebuggerDisplay("{Name}")]
    public class Descriptor : IEquatable<Descriptor>
    {
        internal const BindingFlags OnValueSyncedCallbackBindingFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // NOTE: Default values of fields must match their serialized values, e.g. Unity serializes string
        // as "", not as null, so the initial value after construction must be "". Otherwise,
        // the `Equals` method will not work correctly.
        [SerializeField] private string name;
        [SerializeField] private string monoAssemblyType = string.Empty;

        [SerializeField] private bool required;
        [SerializeField] private bool enforcesLODingWhenFieldsOverriden;
        [SerializeField] private string valueSyncCallbackName = string.Empty;
        [SerializeField] private MemberTypes memberType;
        [SerializeField] private List<string> parameterAssemblyTypes = new();
        [SerializeField] private SerializableType ownerType;
        [SerializeField] private SyncMode defaultSyncMode = SyncMode.Always;

        /// <summary>
        /// CustomData can be used by a BindingProvider to provide additional data to a custom binding,
        /// e.g., an id, hash or name to be use by the <see cref="ValueBinding{T}.Value" /> implementation.
        /// The object needs to be <see cref="System.SerializableAttribute">Serializable</see> and must not be a value type.
        /// For more information, see <see cref="SerializeReference" />.
        /// </summary>
        [SerializeReference] public object CustomData;

        [SerializeField] private SerializableType bindingType;

        [SerializeField] private string oldName;
        [SerializeField] private List<string> oldParameterAssemblyTypes = new List<string>();

        private bool methodCompatible;
        private MessageTarget defaultRouting;
        private int? cachedHashCode;

        public string Name => name;
        public string MonoAssemblyType => monoAssemblyType;

        public bool MethodCompatible => methodCompatible;

        public virtual string BakedCSharpType => TypeUtils.GetMemoizedType(monoAssemblyType).FullName;
        public bool IsMethod => memberType == MemberTypes.Method;
        public bool Required { get => required; internal set => required = value; }
        public bool EnforcesLODingWhenFieldsOverriden => enforcesLODingWhenFieldsOverriden;
        public string ValueSyncCallbackName { get => valueSyncCallbackName; internal set => valueSyncCallbackName = value; }
        public List<string> ParameterAssemblyTypes => parameterAssemblyTypes;
        public MemberTypes MemberType => memberType;
        public Type BindingType => bindingType;
        public Type OwnerType => ownerType;
        public string OwnerAssemblyQualifiedName => ownerType.AssemblyQualifiedName;
        public MessageTarget DefaultRouting => defaultRouting;
        public SyncMode DefaultSyncMode => defaultSyncMode;

        private Coherence.Log.Logger logger = Log.GetLogger<Descriptor>();

        /// <summary>
        /// Rich text representation of the binding's name and type that is displayed in the Configure and Optimize binding windows.
        /// </summary>
        public virtual string Signature => memberType == MemberTypes.Method ? $"{Name}<color=grey>({string.Join(", ", GetParameterAssemblyRuntimeTypes().Select(TypeUtils.GetNiceTypeString))})</color>" : $"{Name} <color=grey>{TypeUtils.GetNiceTypeString(Type.GetType(monoAssemblyType))}</color>";

        public Descriptor(Type ownerType, MemberInfo memberInfo)
        {
            this.name = memberInfo.Name;
            required = memberInfo.IsDefined(typeof(SyncAttribute), true);

            var fieldOrPropertyType = TypeUtils.GetFieldOrPropertyType(memberInfo);
            monoAssemblyType = TypeUtils.TidyAssemblyTypeName(fieldOrPropertyType.AssemblyQualifiedName) ?? string.Empty;
            enforcesLODingWhenFieldsOverriden = true;
            bindingType = new SerializableType(TypeUtils.GetBindingType(fieldOrPropertyType));
            memberType = memberInfo.MemberType;
            this.ownerType = new SerializableType(ownerType);
            valueSyncCallbackName = GetCallbackNameFromAttribute(memberInfo) ?? string.Empty;

            if (required)
            {
                var attr = memberInfo.GetCustomAttribute<SyncAttribute>();
                oldName = attr.OldName;
                defaultSyncMode = attr.DefaultSyncMode;
            }
        }

        public Descriptor(Type ownerType, MethodInfo methodInfo)
        {
            this.name = methodInfo.Name;

            required = methodInfo.IsDefined(typeof(CommandAttribute), true);
            if (required)
            {
                HandleRequiredCommand(methodInfo);
            }

            parameterAssemblyTypes = new List<string>();
            foreach (var pi in methodInfo.GetParameters())
            {
                var assemblyTypeName = TypeUtils.TidyAssemblyTypeName(pi.ParameterType.AssemblyQualifiedName);
                parameterAssemblyTypes.Add(assemblyTypeName);
            }

            defaultRouting = MessageTarget.All;
            bindingType = new SerializableType(typeof(CommandBinding));
            memberType = methodInfo.MemberType;
            this.ownerType = new SerializableType(ownerType);
            methodCompatible = TypeUtils.IsMethodCompatible(methodInfo);
        }

        public Descriptor(string name, Type ownerType, Type bindingType, bool required = false)
        {
            this.name = name ?? string.Empty;
            var paramType = GetParameterTypeFromBindingType(bindingType);
            if (paramType != null)
            {
                this.monoAssemblyType = TypeUtils.TidyAssemblyTypeName(paramType.AssemblyQualifiedName) ?? string.Empty;
            }
            else
            {
                logger.Warning(Warning.ToolkitBindingDescriptorFailedToExtractParameterType,
                    $"Descriptor {name} could not extract parameter type from {bindingType}.");
            }
            enforcesLODingWhenFieldsOverriden = true;
            this.bindingType = new SerializableType(bindingType);
            this.ownerType = new SerializableType(ownerType);
            this.required = required;
            this.memberType = MemberTypes.Custom;
        }

        private void HandleRequiredCommand(MethodInfo methodInfo)
        {
            var attribute = methodInfo.GetCustomAttribute<CommandAttribute>();
            defaultRouting = attribute.defaultRouting;
            oldName = attribute.OldName;

            if (attribute.OldParams != null)
            {
                foreach (var oldType in attribute.OldParams)
                {
                    var assemblyTypeName = TypeUtils.TidyAssemblyTypeName(oldType.AssemblyQualifiedName);
                    oldParameterAssemblyTypes.Add(assemblyTypeName);
                }
            }
        }

        public static bool operator ==(Descriptor obj1, Descriptor obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(Descriptor obj1, Descriptor obj2) => !(obj1 == obj2);

        public override bool Equals(object obj) => obj is Descriptor o && Equals(o);

        public bool Equals(Descriptor other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return name == other.name &&
                   monoAssemblyType == other.monoAssemblyType &&
                   valueSyncCallbackName == other.valueSyncCallbackName &&
                   enforcesLODingWhenFieldsOverriden == other.enforcesLODingWhenFieldsOverriden &&
                   ownerType == other.ownerType &&
                   memberType == other.memberType &&
                   parameterAssemblyTypes.SequenceEqual(other.parameterAssemblyTypes);
        }

        private int GenerateHashCode()
        {
            unchecked
            {
                var hashCode = !string.IsNullOrEmpty(monoAssemblyType) ? monoAssemblyType.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (!string.IsNullOrEmpty(valueSyncCallbackName) ? valueSyncCallbackName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ enforcesLODingWhenFieldsOverriden.GetHashCode();
                hashCode = (hashCode * 397) ^ ownerType?.ToType?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ name.GetHashCode();
                hashCode = (hashCode * 397) ^ memberType.GetHashCode();

                if (parameterAssemblyTypes != null)
                {
                    foreach (var parameterAssemblyType in parameterAssemblyTypes)
                    {
                        hashCode = (hashCode * 397) ^ parameterAssemblyType.GetHashCode();
                    }
                }

                return hashCode;
            }
        }

        public override int GetHashCode()
        {
            if (cachedHashCode.HasValue)
            {
                return cachedHashCode.Value;
            }

            var hashCode = GenerateHashCode();
            cachedHashCode = hashCode;
            return hashCode;
        }

        public bool IsDescriptorRelated(Descriptor other)
        {
            if ((other.IsMethod && !IsMethod) || (!other.IsMethod && IsMethod))
            {
                return false;
            }

            if (other.IsMethod && IsMethod)
            {
                return IsMethodDescriptorRelated(other);
            }

            if (other.oldName?.Equals(name) ?? false)
            {
                return true;
            }

            return other.name.Equals(name) && other.ownerType == ownerType && other.memberType == memberType &&
                   ((other.defaultSyncMode != defaultSyncMode) || !other.monoAssemblyType.Equals(monoAssemblyType) || (!other.valueSyncCallbackName?.Equals(valueSyncCallbackName) ?? false));
        }

        public bool ShouldDefaultToNoneInterpolation()
        {
            // default interpolation should be None if the binding is not in the Transform component
            return ownerType != typeof(Transform);
        }

        internal Binding InstantiateBinding(Component component)
        {
            return Activator.CreateInstance(bindingType, this, component) as Binding;
        }

        private bool IsMethodDescriptorRelated(Descriptor other)
        {
            if (string.IsNullOrEmpty(other.oldName) || !other.oldName.Equals(name))
            {
                return false;
            }

            if (other.oldParameterAssemblyTypes.Count != parameterAssemblyTypes.Count)
            {
                return false;
            }

            bool paramsMatch = true;

            for (int i = 0; i < other.oldParameterAssemblyTypes.Count; i++)
            {
                var old = other.oldParameterAssemblyTypes[i];
                var current = parameterAssemblyTypes[i];

                paramsMatch &= old.Equals(current);
            }

            return paramsMatch;
        }

        private List<Type> GetParameterAssemblyRuntimeTypes()
        {
            var parameterAssemblyRuntimeTypes = new List<Type>();
            foreach (var parameterAssemblyType in parameterAssemblyTypes)
            {
                parameterAssemblyRuntimeTypes.Add(Type.GetType(parameterAssemblyType));
            }

            return parameterAssemblyRuntimeTypes;
        }

        private string GetCallbackNameFromAttribute(MemberInfo memberInfo)
        {
            var callbackAttribute = memberInfo.GetCustomAttribute<OnValueSyncedAttribute>();
            if (callbackAttribute == null)
            {
                return null;
            }

            var valueType = GetValueType(memberInfo);

            if (!TypeUtils.IsBindingSupportingCallbacks(valueType) && !callbackAttribute.SuppressNotBoundError)
            {
                logger.Error(Error.ToolkitBindingDescriptorInvalidType,
                    $"Callback method '{callbackAttribute.Callback}' targets invalid binding type {TypeUtils.GetNiceTypeString(valueType)}. " +
                        $"To suppress this error please use the '{nameof(OnValueSyncedAttribute)}.{nameof(OnValueSyncedAttribute.SuppressNotBoundError)}");

                return null;
            }

            var methodInfo = ownerType.ToType
                .GetMethods(OnValueSyncedCallbackBindingFlags)
                .FirstOrDefault(m => m.Name == callbackAttribute.Callback);
            if (methodInfo == null)
            {
                if (!callbackAttribute.SuppressNotBoundError)
                {
                    logger.Error(Error.ToolkitBindingDescriptorMissingCallback,
                        $"Callback method '{callbackAttribute.Callback}' not found. " +
                        $"To suppress this error please use the '{nameof(OnValueSyncedAttribute)}.{nameof(OnValueSyncedAttribute.SuppressNotBoundError)}");
                }

                return null;
            }

            if (!TypeUtils.CallbackHasValidSignature(methodInfo, valueType))
            {
                if (!callbackAttribute.SuppressNotBoundError)
                {
                    logger.Error(Error.ToolkitBindingDescriptorInvalidCallback,
                        $"Callback method '{callbackAttribute.Callback}' has invalid signature. " +
                            $"Please make sure that the callback method has exactly 2 arguments of type {TypeUtils.GetNiceTypeString(valueType)}. " +
                            $"To suppress this error please use the '{nameof(OnValueSyncedAttribute)}.{nameof(OnValueSyncedAttribute.SuppressNotBoundError)}");
                }
                return null;
            }

            var parameterOrderError = TypeUtils.CheckCallbackParameterOrder(methodInfo);

            if (!string.IsNullOrEmpty(parameterOrderError) && !callbackAttribute.SuppressParamOrderError)
            {
                logger.Error(Error.ToolkitBindingDescriptorParamOrder,
                    ("error", parameterOrderError));
            }

            return methodInfo.Name;
        }

        private Type GetValueType(MemberInfo memberInfo)
        {
            return memberType switch
            {
                MemberTypes.Field => (memberInfo as FieldInfo).FieldType,
                MemberTypes.Property => (memberInfo as PropertyInfo).PropertyType,
                MemberTypes.Custom => null,
                _ => throw new Exception("Unexpected member kind: " + memberType)
            };
        }

        private static Type GetParameterTypeFromBindingType(Type bindingType)
        {
            while (bindingType != null && bindingType != typeof(object))
            {
                if (bindingType.IsGenericType && bindingType.GetGenericTypeDefinition() == typeof(ValueBinding<>))
                {
                    return bindingType.GetGenericArguments()[0];
                }
                bindingType = bindingType.BaseType;
            }
            return null;
        }
    }
}
