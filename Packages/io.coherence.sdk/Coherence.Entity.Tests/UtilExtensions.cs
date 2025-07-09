// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entity.Tests
{
    using Entities;

    public static class UtilExtensions
    {
        public static bool HasComponent(this DeltaComponents components, uint componentID)
        {
            return components.Updates.Store.ContainsKey(componentID);
        }

        public static bool HasDestroy(this DeltaComponents components, uint componentID)
        {
            return components.Destroys.Contains(componentID);
        }

        public static bool TryGetComponent<T>(this DeltaComponents components, out T component)
            where T : struct, ICoherenceComponentData
        {
            var comp = default(T);

            if (components.Updates.Store.TryGetValue(comp.GetComponentType(), out ComponentChange change))
            {
                component = (T)change.Data;
                return true;
            }

            component = default;
            return false;
        }
    }
}
