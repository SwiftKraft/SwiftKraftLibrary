// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit.BindingProviders
{
    using System;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Coherence.Toolkit.Bindings.TransformBindings;
    using UnityEngine;

    [DescriptorProvider(typeof(Transform), -1)]
    internal class TransformDescriptorProvider : DescriptorProvider
    {
        public override bool EmitSchemaComponentDefinition => !IsRootComponent;
        public override bool AssociateCoherenceComponentTypePerBinding => IsRootComponent;

        public override Type MonoComponentReferenceTypeOverride => IsRootComponent ? typeof(CoherenceSync) : typeof(Transform);
        public override string MonoComponentReferenceFieldNameOverride => IsRootComponent ? "_transformViaCoherenceSync" : "_transform";

        private static readonly Dictionary<(Type, string), List<Descriptor>> transformDescriptorCache = new Dictionary<(Type, string), List<Descriptor>>();

        private Type GetCachedProviderType => cachedType ??= GetType();

        private Type cachedType;

        private const string RootKey = "Root";
        private const string ChildKey = "Child";

        public override List<Descriptor> Fetch()
        {
            List<Descriptor> cachedDescriptors = null;

            var cacheKey = IsRootComponent ? RootKey : ChildKey;

            if (transformDescriptorCache.TryGetValue((GetCachedProviderType, cacheKey), out cachedDescriptors))
            {
                return cachedDescriptors;
            }

            cachedDescriptors = new List<Descriptor>();

            transformDescriptorCache.Add((GetCachedProviderType, cacheKey), cachedDescriptors);

            if (IsRootComponent)
            {
                cachedDescriptors.Add(new Descriptor(nameof(Transform.position), Component.GetType(),typeof(PositionBinding), true));
                cachedDescriptors.Add(new Descriptor(nameof(Transform.rotation), Component.GetType(), typeof(RotationBinding)));
                cachedDescriptors.Add(new Descriptor(nameof(Transform.localScale), Component.GetType(), typeof(ScaleBinding)));
            }
            else
            {
                cachedDescriptors.Add(new Descriptor(nameof(Transform.position), Component.GetType(), typeof(DeepPositionBinding)));
                cachedDescriptors.Add(new Descriptor(nameof(Transform.rotation), Component.GetType(), typeof(DeepRotationBinding)));
                cachedDescriptors.Add(new Descriptor(nameof(Transform.localScale), Component.GetType(), typeof(DeepScaleBinding)));
            }

            return cachedDescriptors;
        }
    }
}
