// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entity.Tests
{
    using Entities;
    using System;

    public class IncomingEntityUpdateBuilder
    {
        public static Entity DefaultEntity => new Entity(1, 0, false);

        private IncomingEntityUpdate update = IncomingEntityUpdate.New();
        private readonly DeltaComponentsBuilder componentsBuilder = new();

        public IncomingEntityUpdateBuilder(EntityOperation operation, Entity? entity = null)
        {
            update.Meta.HasMeta = true;
            update.Meta.Operation = operation;
            update.Meta.EntityId = entity.GetValueOrDefault(DefaultEntity);
        }

        public IncomingEntityUpdate Build()
        {
            update.Components = componentsBuilder.Build();
            return update;
        }

        public IncomingEntityUpdateBuilder Components(Action<DeltaComponentsBuilder> build)
        {
            build(componentsBuilder);
            return this;
        }
    }
}
