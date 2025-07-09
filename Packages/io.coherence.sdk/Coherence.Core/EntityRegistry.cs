// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using Entities;
    using System.Collections.Generic;

    public interface IEntityRegistry
    {
        bool EntityExists(in Entity entity);
    }

    internal class EntityRegistry : IEntityRegistry
    {
        private readonly HashSet<Entity> knownEntities;

        public EntityRegistry(HashSet<Entity> knownEntities)
        {
            this.knownEntities = knownEntities;
        }


        public bool EntityExists(in Entity entity)
        {
            return knownEntities.Contains(entity);
        }
    }
}
