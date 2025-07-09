// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    public struct EntityChange
    {
        public Entity ID;
        public OutgoingEntityUpdate Update;
        public SerializedMeta Meta;
    }

    public ref struct EntityRemoveChange
    {
        public Entity ID;
        public uint[] Remove;
        public long Priority;
    }

    public ref struct EntityUpdateChange
    {
        public Entity ID;
        public ComponentUpdates Data;
        public long Priority;
    }

    public ref struct EntityCreateChange
    {
        public Entity ID;
        public ComponentUpdates Data;
    }
}
