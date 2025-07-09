// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Channels
{
    using System;
    using System.Collections.Generic;
    using Coherence.Brook;
    using Coherence.Entities;
    using Coherence.ProtocolDef;
    using Coherence.Serializer;
    using Coherence.SimulationFrame;

    /// <summary>
    /// A channel for outgoing entity changes/commands/inputs. It's responsible for
    /// buffering the changes and serializing them.
    /// </summary>
    internal interface IOutNetworkChannel
    {
        event Action<Entity> OnEntityAcked;

        void CreateEntity(Entity id, ICoherenceComponentData[] data);
        void UpdateComponents(Entity id, ICoherenceComponentData[] data);
        void RemoveComponents(Entity id, uint[] componentTypes, Dictionary<Entity, HashSet<uint>> ackedComponentsPerEntity);
        void DestroyEntity(Entity id, IReadOnlyCollection<Entity> ackedEntities);

        void PushCommand(IEntityCommand message, MessageTarget target, Entity id, bool useDebugStreams);
        void PushInput(IEntityInput message, bool useDebugStreams);

        /// <summary>
        /// Returns true if there is a change for the given entity (pending or in-flight).
        /// </summary>
        bool HasChangesForEntity(Entity entity);

        /// <summary>
        /// Drops all changes for the given entity (pending and in-flight).
        /// </summary>
        void ClearAllChangesForEntity(Entity entity);

        bool HasChanges(IReadOnlyCollection<Entity> ackedEntities);

        /// <summary>
        /// Serializes pending changes, leaving enough space for EndOfChannels.
        /// </summary>
        /// <returns>True if any change was serialized</returns>
        bool Serialize(SerializerContext<IOutBitStream> serializerCtx, AbsoluteSimulationFrame referenceSimulationFrame,
            bool holdOnToCommands, IReadOnlyCollection<Entity> ackedEntities);

        Dictionary<Entity, OutgoingEntityUpdate> MarkAsSent(SequenceId packetSequenceId);

        void OnDeliveryInfo(DeliveryInfo info, ref HashSet<Entity> ackedEntities,  ref Dictionary<Entity, HashSet<uint>> ackedComponentsPerEntity);

        void Reset();

        void ClearLastSerializationResult();
    }
}
