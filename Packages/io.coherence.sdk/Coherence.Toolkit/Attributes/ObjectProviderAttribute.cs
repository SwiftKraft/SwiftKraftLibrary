// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class ObjectProviderAttribute : PropertyAttribute
    {
        public Type Type { get; }
        public string PrefixLabel { get; }
        public string Tooltip { get; }

        private static readonly List<Type> allowedTypes = new List<Type>()
        {
            typeof(INetworkObjectInstantiator), typeof(INetworkObjectProvider)
        };
        
        public ObjectProviderAttribute(Type type, string prefixLabel, string tooltip)
        {
            if (!allowedTypes.Contains(type))
            {
                throw new ArgumentException($"Type {type.Name} is not supported by this attribute.");
            }
            
            Type = type;
            PrefixLabel = prefixLabel;
            Tooltip = tooltip;
        }
    }
}
