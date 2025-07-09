// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Entities;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Caches ICoherenceSyncUpdater instances grouped by CoherenceLoopConfig to provide quick access to all networked entities at each point in the update loop.
    /// Keeps three separate collections for Update, FixedUpdate and LateUpdate which should be slightly faster than a dictionary lookup.
    /// </summary>
    internal class CoherenceLoopLookup
    {
        private readonly Dictionary<Entity, ICoherenceSyncUpdater> updateLookup = new Dictionary<Entity, ICoherenceSyncUpdater>();
        private readonly Dictionary<Entity, ICoherenceSyncUpdater> lateUpdateLookup = new Dictionary<Entity, ICoherenceSyncUpdater>();
        private readonly Dictionary<Entity, ICoherenceSyncUpdater> fixedUpdateLookup = new Dictionary<Entity, ICoherenceSyncUpdater>();

        private readonly ReadOnlyDictionary<Entity, ICoherenceSyncUpdater> updateLookupReadOnly;
        private readonly ReadOnlyDictionary<Entity, ICoherenceSyncUpdater> lateUpdateLookupReadOnly;
        private readonly ReadOnlyDictionary<Entity, ICoherenceSyncUpdater> fixedUpdateLookupReadOnly;

        public CoherenceLoopLookup()
        {
            updateLookupReadOnly = new ReadOnlyDictionary<Entity, ICoherenceSyncUpdater>(updateLookup);
            lateUpdateLookupReadOnly = new ReadOnlyDictionary<Entity, ICoherenceSyncUpdater>(lateUpdateLookup);
            fixedUpdateLookupReadOnly = new ReadOnlyDictionary<Entity, ICoherenceSyncUpdater>(fixedUpdateLookup);
        }

        public ReadOnlyDictionary<Entity, ICoherenceSyncUpdater> Get(CoherenceSync.InterpolationLoop loop)
        {
            return loop switch
            {
                CoherenceSync.InterpolationLoop.Update => updateLookupReadOnly,
                CoherenceSync.InterpolationLoop.LateUpdate => lateUpdateLookupReadOnly,
                CoherenceSync.InterpolationLoop.FixedUpdate => fixedUpdateLookupReadOnly,
                var _ => throw new ArgumentOutOfRangeException(nameof(loop), loop, null)
            };
        }

        public void Add(Entity id, ICoherenceSyncUpdater updater, CoherenceSync.InterpolationLoop interpolationLocation)
        {
            if (updater == null)
            {
                return;
            }

            if (interpolationLocation.HasFlag(CoherenceSync.InterpolationLoop.Update))
            {
                updateLookup.Add(id, updater);
            }

            if (interpolationLocation.HasFlag(CoherenceSync.InterpolationLoop.LateUpdate))
            {
                lateUpdateLookup.Add(id, updater);
            }

            if (interpolationLocation.HasFlag(CoherenceSync.InterpolationLoop.FixedUpdate))
            {
                fixedUpdateLookup.Add(id, updater);
            }
        }

        public void Remove(Entity id, CoherenceSync.InterpolationLoop interpolationLocation)
        {
            if (interpolationLocation.HasFlag(CoherenceSync.InterpolationLoop.Update))
            {
                updateLookup.Remove(id);
            }

            if (interpolationLocation.HasFlag(CoherenceSync.InterpolationLoop.LateUpdate))
            {
                lateUpdateLookup.Remove(id);
            }

            if (interpolationLocation.HasFlag(CoherenceSync.InterpolationLoop.FixedUpdate))
            {
                fixedUpdateLookup.Remove(id);
            }
        }

        public void Clear()
        {
            updateLookup.Clear();
            fixedUpdateLookup.Clear();
            lateUpdateLookup.Clear();
        }
    }
}
