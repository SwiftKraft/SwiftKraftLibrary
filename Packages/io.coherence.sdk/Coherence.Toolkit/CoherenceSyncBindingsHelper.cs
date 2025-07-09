// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Bindings;
    using UnityEngine;

    internal static class CoherenceSyncBindingHelper
    {
        public static Binding AddBinding(CoherenceSync sync, Component component, Descriptor descriptor)
        {
            if (!sync.HasBindingForDescriptor(descriptor, component))
            {
                var newBinding = descriptor.InstantiateBinding(component);
                sync.Bindings.Add(newBinding);
                sync.ValidateArchetype();
                return newBinding;
            }

            return null;
        }

        public static bool RemoveBinding(CoherenceSync sync, Component component, Descriptor descriptor)
        {
            var binding = sync.GetBindingForDescriptor(descriptor, component);

            var removed = sync.Bindings.Remove(binding);

            if (removed)
            {
                sync.ValidateArchetype();
            }

            return removed;
        }

        public static Binding GetSerializedBinding(CoherenceSync sync, Binding binding)
        {
            foreach (Binding serializedBinding in sync.Bindings)
            {
                if (binding.Equals(serializedBinding))
                {
                    return serializedBinding;
                }
            }
            return null;
        }
    }
}
