// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Coherence.Toolkit.Bindings;
    using System.Linq;
    using Coherence.Toolkit;
    using Toolkit.BindingValidators;
    using UnityEngine;

    public class DescriptorProvider
    {
        private readonly MethodBindingValidator methodValidator = new();

        // Storing reference as GameObject to not trigger Unity 2020 bug
        // that leads to null references after deserializing CoherenceSync
        // references after editor startup.
        public GameObject Root { get; internal set; }

        public Component Component { get; private set; }

        public virtual bool IsRootComponent => Component.gameObject.transform.parent == null || Component.gameObject.transform.parent.name.Equals("Canvas (Environment)");

        public virtual bool EmitSchemaComponentDefinition => true;
        public virtual string SchemaComponentNameOverride => null;

        public virtual bool AssociateCoherenceComponentTypePerBinding => false;

        public virtual bool EmitMonoComponentReferenceOnBakedSyncScript => true;
        public virtual string MonoComponentReferenceFieldNameOverride => null;
        public virtual Type MonoComponentReferenceTypeOverride => null;

        private static readonly Dictionary<Type, List<Descriptor>> descriptorCache = new Dictionary<Type, List<Descriptor>>();

        internal void SetComponent(Component component)
        {
            Component = component;
        }

        protected virtual bool CustomFieldFilter(FieldInfo fieldInfo)
        {
            return true;
        }

        protected virtual bool CustomPropertyFilter(PropertyInfo propertyInfo)
        {
            return true;
        }

        protected virtual bool CustomMethodFilter(MethodInfo methodInfo)
        {
            return true;
        }

        public virtual List<Descriptor> Fetch()
        {
            if (!descriptorCache.TryGetValue(Component.GetType(), out var cachedDescriptors))
            {
                cachedDescriptors = GetDescriptorsUsingReflection();
                descriptorCache.Add(Component.GetType(), cachedDescriptors);
            }

            return new List<Descriptor>(cachedDescriptors);
        }

        private List<Descriptor> GetDescriptorsUsingReflection()
        {
            var descriptors = new List<Descriptor>();

            // Fields
            descriptors.AddRange(Component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).
                Where(memberInfo => memberInfo.IsValidBinding() && CustomFieldFilter(memberInfo)).
                Select(memberInfo => new Descriptor(Component.GetType(), memberInfo)));

            // Properties
            descriptors.AddRange(Component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).
                Where(memberInfo => memberInfo.IsValidBinding() && CustomPropertyFilter(memberInfo)).
                Select(memberInfo => new Descriptor(Component.GetType(), memberInfo)));

            // Methods
            descriptors.AddRange(Component.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).
                Where(methodInfo => methodValidator.AssertBindingIsValid(methodInfo) && CustomMethodFilter(methodInfo)).
                Select(methodInfo => new Descriptor(Component.GetType(), methodInfo)));

            return descriptors;
        }

        public virtual GUIContent GetIconContent(Descriptor descriptor)
        {
            return GUIContent.none;
        }

        public virtual MenuItemData[] AdditionalMenuItemData => null;
    }
}
