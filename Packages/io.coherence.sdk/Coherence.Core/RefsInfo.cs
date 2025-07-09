// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System.Collections.Generic;
    using Entities;

    internal struct RefsInfo
    {
        private static readonly List<Entity> EmptyRefs = new();

        public Entity Referer;
        public IReadOnlyList<Entity> ReferencedEntities => referencedEntities;
        private readonly List<Entity> referencedEntities;

        public bool HasAnyRefs => ReferencedEntities?.Count > 0;

        public RefsInfo(in IncomingEntityUpdate update)
        {
            Referer = update.Entity;
            referencedEntities = null;

            var values = update.Components.Updates.Store.SortedValues;
            for (var i = 0; i < values.Count; i++)
            {
                var change = values[i];
                if (change.Data.HasRefFields())
                {
                    var entityRefs = change.Data.GetEntityRefs();
                    referencedEntities ??= new List<Entity>(entityRefs.Count);
                    referencedEntities.AddRange(entityRefs);
                }
            }

            referencedEntities ??= EmptyRefs;
        }

        public RefsInfo(in Entity referer, List<Entity> referencedEntities)
        {
            Referer = referer;
            this.referencedEntities = referencedEntities;
        }
    }
}
