// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public struct DeltaComponents
    {
        public ComponentUpdates Updates;
        public HashSet<uint> Destroys;
        public int Count => Updates.Count + Destroys.Count;
        internal bool IsInitialized => Destroys != null;

        /// <summary>
        /// The time at which the last ordered component was updated at.
        /// Or null if the components were acked.
        /// </summary>
        public DateTime? OrderedUpdateTime;

        public static DeltaComponents New(int capacity = 16)
        {
            var deltaComponents = new DeltaComponents();
            deltaComponents.EnsureInitialized(capacity);
            return deltaComponents;
        }

        internal void EnsureInitialized(int capacity = 16)
        {
            if (IsInitialized)
            {
                return;
            }

            Updates = ComponentUpdates.New(capacity);
            Destroys = new HashSet<uint>(capacity / 4);
        }

        public void CloneFrom(DeltaComponents other)
        {
            foreach (var change in other.Updates.Store)
            {
                Updates.Store.Add(change.Key, change.Value.Clone());
            }

            Destroys.UnionWith(other.Destroys);

            OrderedUpdateTime = MergeOrderedUpdateTime(OrderedUpdateTime, other.OrderedUpdateTime);
        }

        public DeltaComponents Clone()
        {
            var clone = new DeltaComponents
            {
                Updates = Updates.Clone(),
                Destroys = new HashSet<uint>(Destroys),
                OrderedUpdateTime = OrderedUpdateTime,
            };

            return clone;
        }

        public void UpdateComponent(ComponentChange change)
        {
            Destroys.Remove(change.Data.GetComponentType());
            Updates.Update(change);
        }

        public void UpdateComponents(ComponentUpdates componentUpdates)
        {
            var componentChanges = componentUpdates.Store.SortedValues;
            for (var i = 0; i < componentChanges.Count; i++)
            {
                UpdateComponent(componentChanges[i]);
            }
        }

        public void RemoveComponent(uint comp)
        {
            Destroys.Add(comp);
            Updates.Remove(comp);
        }

        public void RemoveComponents(IReadOnlyList<uint> components)
        {
            for (var i = 0; i < components.Count; i++)
            {
                RemoveComponent(components[i]);
            }
        }

        public void Merge(DeltaComponents other)
        {
            foreach (var destructs in other.Destroys)
            {
                RemoveComponent(destructs);
            }

            foreach (var update in other.Updates.Store)
            {
                UpdateComponent(update.Value);
            }

            OrderedUpdateTime = MergeOrderedUpdateTime(OrderedUpdateTime, other.OrderedUpdateTime);
        }

        /// <summary>
        /// Returns true if any components (updated or removed) is marked as ordered.
        /// </summary>
        public bool ContainsOrderedComponent(IComponentInfo componentInfo)
        {
            foreach (var destructs in Destroys)
            {
                if (componentInfo.IsSendOrderedComponent(destructs))
                {
                    return true;
                }
            }

            return Updates.ContainsOrderedComponent();
        }

        public bool HasUnackedOrderedComponents()
        {
            return OrderedUpdateTime != null;
        }

        public void Reset()
        {
            Updates.Reset();
            Destroys.Clear();
            OrderedUpdateTime = null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(256);
            if (Updates.Count > 0)
            {
                sb.Append("Updates: ");
                sb.Append(Updates);
            }

            if (Destroys.Count > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append("Destroys: ");
                sb.Append(string.Join(", ", Destroys));
            }

            if (sb.Length == 0)
            {
                sb.Append("Empty");
            }

            if (OrderedUpdateTime != null)
            {
                sb.Append(" OrderedUpdateTime: ");
                sb.Append(OrderedUpdateTime);
            }

            return sb.ToString();
        }

        public static DateTime? MergeOrderedUpdateTime(DateTime? first, DateTime? second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            if (first.Value > second.Value)
            {
                return first;
            }

            return second;
        }
    }
}
