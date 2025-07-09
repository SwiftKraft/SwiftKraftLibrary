// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    public struct ComponentUpdates
    {
        public SortedValueMap<uint, ComponentChange> Store { get; private set; }
        public int Count => Store.Count;
        public Vector3 FloatingOriginDelta;

        public static ComponentUpdates New(int capacity)
        {
            return new ComponentUpdates
            {
                Store = new SortedValueMap<uint, ComponentChange>(ComponentChangeComparer.Cached, capacity),
            };
        }

        public static ComponentUpdates New(IDictionary<uint, ComponentChange> componentChanges)
        {
            return new ComponentUpdates
            {
                Store = new SortedValueMap<uint, ComponentChange>(ComponentChangeComparer.Cached,
                    componentChanges),
            };
        }

        public static ComponentUpdates New(IReadOnlyList<ICoherenceComponentData> data)
        {
            var store = new SortedValueMap<uint, ComponentChange>(ComponentChangeComparer.Cached, data.Count);

            for (var i = 0; i < data.Count; i++)
            {
                var componentData = data[i];

                store[componentData.GetComponentType()] = ComponentChange.New(componentData);
            }

            return new ComponentUpdates
            {
                Store = store,
            };
        }

        public ComponentUpdates Clone()
        {
            var clone = new ComponentUpdates
            {
                Store = new SortedValueMap<uint, ComponentChange>(ComponentChangeComparer.Cached, Store),
                FloatingOriginDelta = FloatingOriginDelta,
            };

            return clone;
        }

        public void ClearMask(ComponentChange change)
        {
            var componentType = change.Data.GetComponentType();
            if (!Store.TryGetValue(componentType, out var componentChange))
            {
                return;
            }

            var clearedComponent = componentChange.ClearMask(change.Data.FieldsMask);
            if (clearedComponent.Data.FieldsMask == 0)
            {
                // Component has no fields to send. This happens when the same component with newer data
                // is already in flight so it's safe to simply remove it - no reason to send empty component.
                Store.Remove(componentType);
            }
            else
            {
                // also clear the stopped mask for removed fields
                // since there is a new update which means it is no
                // longer stopped in this update, but the existing update
                // might have a stop which should merge correctly.
                // This uses the change.Data.FieldsMask to know which fields have
                // new values in them to clear the stopped mask.
                clearedComponent = clearedComponent.ClearStoppedMask(change.Data.FieldsMask);

                Store[componentType] = clearedComponent;
            }
        }

        public void Update(ComponentChange change)
        {
            var componentType = change.Data.GetComponentType();
            if (Store.TryGetValue(componentType, out var existingChange))
            {
                Store[componentType] = existingChange.Update(change);
            }
            else
            {
                Store.Add(componentType, change);
            }
        }

        public void Reset()
        {
            Store.Clear();
        }

        public void Remove(uint componentType)
        {
            Store.Remove(componentType);
        }

        public bool ContainsOrderedComponent()
        {
            foreach (var (_, comp) in Store)
            {
                if (comp.Data.IsSendOrdered())
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            var components = string.Join(", ", Store.Keys);
            var changes = string.Join(", ", Store.SortedValues.Select(v => v.ToString()));
            return $"Comps:[{components}] Store: {changes}";
        }
    }
}
