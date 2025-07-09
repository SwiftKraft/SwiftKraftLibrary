// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Channels
{
    using System;
    using System.Collections.Generic;
    using Brook;
    using Common;
    using Entities;
    using Log;
    using ProtocolDef;
    using Serializer;
    using SimulationFrame;
    using Stats;

    internal class OutOrderedNetworkChannel : IOutNetworkChannel
    {
        public OrderedChannelSerializationResult LastSerializationResult { get; private set; }

        internal static readonly int SequenceBufferSize = 512;


        public event Action<Entity> OnEntityAcked { add { } remove { } }

        private readonly ISchemaSpecificComponentSerialize serializer;
        private readonly IComponentInfo definition;
        private readonly Stats stats;
        private readonly Logger logger;

        private readonly Queue<SerializedEntityMessage> commandQueue = new(32);

        private readonly SendSequenceBuffer sequenceBuffer = new(SequenceBufferSize);
        private readonly SentSequenceCache sentCache = new();

        // Caches messages to send each tick.
        private readonly List<(MessageID, SerializedEntityMessage)> sendMessages = new(32);

        // Cache for MarkAsSent() so we don't need to re-allocate the map every time
        private readonly Dictionary<Entity, OutgoingEntityUpdate> emptyUpdatesSent = new();

        // Cache for OnDeliveryInfo
        private readonly CacheList<MessageID> messageIdBuffer = new(32);

        public OutOrderedNetworkChannel(
            ISchemaSpecificComponentSerialize serializer,
            IComponentInfo definition,
            Stats stats,
            Logger logger)
        {
            this.serializer = serializer;
            this.definition = definition;
            this.stats = stats;
            this.logger = logger.With<OutOrderedNetworkChannel>();
        }

        public void CreateEntity(Entity id, ICoherenceComponentData[] data) =>
            throw new NotImplementedException("CreateEntity not implemented");
        public void UpdateComponents(Entity id, ICoherenceComponentData[] data) =>
            throw new NotImplementedException("UpdateComponents not implemented");

        public void RemoveComponents(Entity id, uint[] componentTypes,
            Dictionary<Entity, HashSet<uint>> ackedComponentsPerEntity) =>
            throw new NotImplementedException("RemoveComponents not implemented");

        public void DestroyEntity(Entity id, IReadOnlyCollection<Entity> ackedEntities) =>
            throw new NotImplementedException("DestroyEntity not implemented");

        public void PushCommand(IEntityCommand message, MessageTarget target, Entity id, bool useDebugStreams)
        {
            var serializedMessage = Serializer.Serialize.SerializeMessage(MessageType.Command, target, message, id, serializer, useDebugStreams, logger);
            if (serializedMessage != null)
            {
                commandQueue.Enqueue(serializedMessage);
            }
        }

        public void PushInput(IEntityInput message, bool useDebugStreams) =>
            throw new NotImplementedException("PushInput not implemented");

        public bool HasChangesForEntity(Entity entity) => false;
        public void ClearAllChangesForEntity(Entity entity) { }

        public bool HasChanges(IReadOnlyCollection<Entity> ackedEntities) => (commandQueue.Count > 0) || !sequenceBuffer.IsEmpty();

        public bool Serialize(SerializerContext<IOutBitStream> serializerCtx,
            AbsoluteSimulationFrame referenceSimulationFrame,
            bool holdOnToCommands, IReadOnlyCollection<Entity> ackedEntities)
        {
            LastSerializationResult = new OrderedChannelSerializationResult()
            {
                MessagesSent = new List<MessageID>(),
            };

            var messages = PreSerialize();
            if (messages.Count == 0 || holdOnToCommands)
            {
                LastSerializationResult = null;

                return false;
            }

            var sentMessages = Serializer.Serialize.WriteOrderedCommands(messages, serializerCtx);

            if (sentMessages.Count == 0)
            {
                LastSerializationResult = null;

                return false;
            }

            LastSerializationResult = new OrderedChannelSerializationResult()
            {
                MessagesSent = sentMessages,
            };

            Serializer.Serialize.WriteEndOfMessages(serializerCtx);

            return true;
        }

        public Dictionary<Entity, OutgoingEntityUpdate> MarkAsSent(SequenceId packetSequenceId)
        {
            if (LastSerializationResult == null)
            {
                sentCache.EnqueueEmpty();

                return emptyUpdatesSent;
            }

            if (LastSerializationResult is not OrderedChannelSerializationResult result)
            {
                throw new Exception("Given serializationResult cannot be casted to OrderedChannelSerializationResult");
            }

            sequenceBuffer.OnMessagesSent(result.MessagesSent);
            sentCache.Enqueue(result.MessagesSent);

            if (stats != null)
            {
                stats.TrackOutgoingMessages(MessageType.Command, result.MessagesSent?.Count ?? 0);
            }

            return emptyUpdatesSent;
        }

        public void OnDeliveryInfo(DeliveryInfo info, ref HashSet<Entity> ackedEntities,
            ref Dictionary<Entity, HashSet<uint>> ackedComponentsPerEntity)
        {
            using var _ = messageIdBuffer;

            if (!sentCache.Dequeue(messageIdBuffer))
            {
                logger.Error(Error.CoreChannelOutOrderedNetworkChannelInvalidAck);
                return;
            }

            sequenceBuffer.OnMessagesDelivered(messageIdBuffer, info.WasDelivered);
        }

        public void Reset()
        {
            commandQueue.Clear();
            sentCache.Clear();
            sequenceBuffer.Clear();
        }

        public void ClearLastSerializationResult()
        {
            this.LastSerializationResult = null;
        }

        private List<(MessageID, SerializedEntityMessage)> PreSerialize()
        {
            // Push pending commands to sequence buffer.
            while (commandQueue.TryPeek(out var message))
            {
                if (sequenceBuffer.AppendMessage(message))
                {
                    commandQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }

            sequenceBuffer.GetMessagesToSend(sendMessages);

            return sendMessages;
        }
    }

    internal class OrderedChannelSerializationResult
    {
        public List<MessageID> MessagesSent;
        public void Clear() => MessagesSent.Clear();
    }
}
