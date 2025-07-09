// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    public class DescriptorProviderAttribute : Attribute
    {
        public readonly Type componentType;
        public readonly int priority;

        public DescriptorProviderAttribute(Type componentType) : this(componentType, 0)
        {
            this.componentType = componentType;
        }

        public DescriptorProviderAttribute(Type componentType, int priority)
        {
            this.componentType = componentType;
            this.priority = priority;
        }
    }
}
