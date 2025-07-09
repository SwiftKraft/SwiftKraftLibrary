// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    using System.Collections.Concurrent;
    using SimulationFrame;

    public struct OutgoingEntityUpdate
    {
        private static readonly ConcurrentBag<OutgoingEntityUpdate> Pool = new();

        public EntityOperation Operation;
        public long Priority;
        public DeltaComponents Components;

        public bool IsDestroy => Operation == EntityOperation.Destroy;
        public bool IsCreate => Operation == EntityOperation.Create;
        public bool IsUpdate => Operation == EntityOperation.Update;

        public bool HasExistenceOperation =>
            Operation == EntityOperation.Create || Operation == EntityOperation.Destroy;

        public static OutgoingEntityUpdate New()
        {
            if (Pool.TryTake(out var instance))
            {
                return instance;
            }

            return new OutgoingEntityUpdate
            {
                Operation = EntityOperation.Unknown,
                Priority = 0,
                Components = DeltaComponents.New(),
            };
        }

        public OutgoingEntityUpdate Clone()
        {
            var clone = New();

            clone.Operation = Operation;
            clone.Priority = Priority;
            clone.Components.CloneFrom(Components);

            return clone;
        }

        public void Return()
        {
            Reset();
            Pool.Add(this);
        }

        private void Reset()
        {
            Operation = EntityOperation.Unknown;
            Priority = 0;
            Components.Reset();
        }

        /// <summary>
        /// Remove all own updated fields that are present in update
        /// This since we don't need to resend fields that have been changed afterwards.
        /// If current or newer entity update is a Destroy operator, return a Destroy.
        /// </summary>
        /// <param name="update">A newer entity update</param>
        public void Subtract(OutgoingEntityUpdate update, IComponentInfo definition)
        {
            if (IsDestroy)
            {
                return;
            }

            if (update.IsDestroy)
            {
                Operation = update.Operation;
                return;
            }

            foreach (var destructs in update.Components.Destroys)
            {
                // remove updates for components that are removed
                Components.Updates.Store.Remove(destructs);

                // remove destructs that already exist
                Components.Destroys.Remove(destructs);
            }

            foreach (var (componentId, change) in update.Components.Updates.Store)
            {
                // remove updates for which updates already exists
                Components.Updates.ClearMask(change);

                // also remove all the removes in the changed state where the existing has an update to the
                // component since the component update overrides the remove.
                Components.Destroys.Remove(componentId);
            }

            if (!Components.ContainsOrderedComponent(definition))
            {
                Components.OrderedUpdateTime = null;
            }
        }

        /// <summary>
        /// Add updated components to own collection.
        /// If an update exists in both, the update will overwrite the owned changes.
        /// If current or newer entity update is a Destroy operator, return a Destroy.
        /// </summary>
        /// <param name="update">A newer entity update</param>
        public void Add(OutgoingEntityUpdate update)
        {
            // Destroy always takes precedence
            if (IsDestroy)
            {
                return;
            }

            if (update.IsDestroy)
            {
                Operation = update.Operation;
                return;
            }

            Components.Merge(update.Components);

            // Dropped create overwrites update
            if (update.IsCreate)
            {
                Operation = update.Operation;
            }
        }

        public new string ToString()
        {
            return $"Op: {Operation} Prio: {Priority} Comps: {Components.ToString()}";
        }
    }
}
