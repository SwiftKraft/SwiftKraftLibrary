// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    public class ComponentActionAttribute : Attribute
    {
        public readonly Type componentType;
        public readonly string name;

        public ComponentActionAttribute(Type componentType) : this(componentType, string.Empty)
        {
            this.componentType = componentType;
        }

        public ComponentActionAttribute(Type componentType, string name)
        {
            this.componentType = componentType;
            this.name = name;
        }
    }
}

