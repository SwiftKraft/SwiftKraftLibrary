// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System.Collections.Generic;
    using Coherence.Entities;

    internal class SentCache
    {
        private LinkedList<ChangeBuffer> sentChanges = new LinkedList<ChangeBuffer>();

        public (ChangeBuffer, LinkedList<ChangeBuffer> inFlight) Dequeue()
        {
            ChangeBuffer changes = null;

            if (sentChanges.Count > 0)
            {
                changes = sentChanges.Last.Value;
                sentChanges.RemoveLast();
            }

            return (changes, sentChanges);
        }

        public void Enqueue(ChangeBuffer changes)
        {
            sentChanges.AddFirst(changes);
        }

        public void ClearAllChangesForEntity(Entity id)
        {
            foreach (var change in sentChanges)
            {
                change.ClearAllChangesForEntity(id);
            }
        }

        public bool HasChangesForEntity(Entity id)
        {
            foreach (var changes in sentChanges)
            {
                if (changes != null && changes.HasChangesForEntity(id))
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearComponentChangesForEntity(Entity id, uint componentID)
        {
            foreach (var changes in sentChanges)
            {
                if (changes != null)
                {
                    changes.ClearComponentChangesForEntity(id, componentID);
                }
            }
        }

        public bool HasComponentChangesForEntity(Entity id, uint componentID)
        {
            foreach (var changes in sentChanges)
            {
                if (changes != null && changes.HasChangesForEntity(id))
                {
                    return true;
                }
            }

            return false;
        }

        public void BumpPriorities()
        {
            foreach (var changeBuffer in sentChanges)
            {
                if (changeBuffer != null)
                {
                    changeBuffer.ReprioritizeChanges(SendChangeBuffer.HELDBACK_PRIORITY);
                }
            }
        }

        public void GetOrderedComponents(Entity entity, IComponentInfo componentInfo, out DeltaComponents? components)
        {
            var comps = new DeltaComponents();

            for (var sentBuffer = sentChanges.Last; sentBuffer != null; sentBuffer = sentBuffer.Previous)
            {
                sentBuffer.Value.MergeIfOrderedComponents(entity, ref comps, componentInfo);
            }

            components = comps.IsInitialized ? comps : null;
        }
    }
}
