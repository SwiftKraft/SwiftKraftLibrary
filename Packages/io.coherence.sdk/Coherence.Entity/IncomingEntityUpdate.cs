// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    public struct IncomingEntityUpdate
    {
        public EntityWithMeta Meta;
        public DeltaComponents Components;

        public Entity Entity => Meta.EntityId;
        public bool IsCreate => Meta.Operation == EntityOperation.Create;
        public bool IsDestroy => Meta.Operation == EntityOperation.Destroy;

        public static IncomingEntityUpdate New(int capacity = 0)
        {
            return new IncomingEntityUpdate
            {
                Components = DeltaComponents.New(capacity),
            };
        }

        public void Merge(in IncomingEntityUpdate other)
        {
            var operation = Meta.Operation.Merge(other.Meta.Operation);

            Meta = other.Meta;
            Meta.Operation = operation;
            Components.Merge(other.Components);
        }

        public override string ToString()
        {
            return $"{nameof(Meta)}: [{Meta}], {nameof(Components)}: [{Components}]";
        }
    }
}
