// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum DestroyReason : byte
    {
        BadReason = 0,
        ClientDestroy,
        DuplicateDestroy,
        QueryMoved,
        MaxEntitiesReached,
        MaxQueriesReached,
        UnauthorizedCreate,
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum EntityOperation : byte
    {
        Unknown = 0,
        Create,
        Update,
        Destroy,
    }

    public static class EntityOperationExtensions
    {
        public static EntityOperation Merge(this EntityOperation currentOperation, EntityOperation newOperation)
        {
            // Destroy always takes precedence
            if (newOperation == EntityOperation.Destroy)
            {
                return newOperation;
            }

            // We don't want to overwrite create with update
            if (currentOperation == EntityOperation.Create)
            {
                return currentOperation;
            }

            return newOperation;
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct EntityWithMeta
    {
        public Entity EntityId;
        public bool HasMeta;
        public bool HasStateAuthority;
        public bool HasInputAuthority;
        public bool IsOrphan;
        public uint LOD;
        public EntityOperation Operation;
        public DestroyReason DestroyReason;

        public bool IsAlive => Operation == EntityOperation.Create || Operation == EntityOperation.Update;
        public bool IsDestroyed => Operation == EntityOperation.Destroy;

        public override string ToString()
        {
            if (!HasMeta)
            {
                return "-";
            }

            return Operation == EntityOperation.Destroy
                ? $"({Operation}) {nameof(EntityId)}: {EntityId}, {nameof(HasStateAuthority)}: {HasStateAuthority}, {nameof(HasInputAuthority)}: {HasInputAuthority}, {nameof(DestroyReason)}: {DestroyReason}"
                : $"({Operation}) {nameof(EntityId)}: {EntityId}, {nameof(HasStateAuthority)}: {HasStateAuthority}, {nameof(HasInputAuthority)}: {HasInputAuthority}, {nameof(IsOrphan)}: {IsOrphan}, {nameof(LOD)}: {LOD}";
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct SerializedMeta
    {
        public byte Version;
        public bool HasStateAuthority;
        public bool HasInputAuthority;
        public bool IsOrphan;
        public uint LOD;
        public EntityOperation Operation;
        public DestroyReason DestroyReason;

        public bool IsDeleted => Operation == EntityOperation.Destroy;
    }
}
