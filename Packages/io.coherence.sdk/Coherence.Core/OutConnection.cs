// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Brook;
    using Coherence.Common;
    using Coherence.Core.Channels;
    using Connection;
    using Entities;
    using Log;
    using ProtocolDef;
    using Serializer;
    using SimulationFrame;

    public class OutConnection
    {
        private readonly SortedList<ChannelID, IOutNetworkChannel> channels = new();
        private IOutNetworkChannel DefaultChannel => channels[ChannelID.Default];
        private readonly IOutConnection connection;

        private HashSet<Entity> ackedEntities;
        private Dictionary<Entity, HashSet<uint>> ackedComponentsPerEntity = new Dictionary<Entity, HashSet<uint>>();

        // When an entity is transferred from this client it is added to this
        // set until the RS acks the transfer.
        private HashSet<Entity> entitiesInAuthTransfer;

        private readonly Logger logger;

        private List<Entity> entitiesToRemove = new List<Entity>();

        private Vector3d floatingOrigin;

        // Cache for SerializeAndQueuePackets so we don't need to re-allocate the map every time
        private readonly Dictionary<ChannelID, Dictionary<Entity, OutgoingEntityUpdate>> allChannelUpdatesSent = new();

        internal event Action<PacketSentDebugInfo> OnPacketSent;
        internal event Action<Entity> OnEntityAcked;

        internal event Action<Entity> OnAuthorityTransferred;

        internal OutConnection(IOutConnection connection,
            Dictionary<ChannelID, IOutNetworkChannel> channels,
            HashSet<Entity> ackedEntities,
            Logger logger)
        {
            this.connection = connection;
            this.ackedEntities = ackedEntities;
            entitiesInAuthTransfer = new HashSet<Entity>(Entity.DefaultComparer);

            this.logger = logger.With<OutConnection>();

            foreach (var (channelID, channel) in channels)
            {
                AddChannel(channelID, channel);
            }
        }

        internal void AddChannel(ChannelID channelID, IOutNetworkChannel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel), "channel must not be null");
            }

            if (!channelID.IsValid())
            {
                throw new ArgumentException($"Invalid ChannelID {channelID}, only channels {ChannelID.MinValue}-{ChannelID.MaxValue} are supported");
            }

            if (!channels.TryAdd(channelID, channel))
            {
                throw new Exception($"Failed to add channel, duplicate ChannelID {channelID}");
            }

            channel.OnEntityAcked += e => OnEntityAcked?.Invoke(e);
        }

        public void Update(AbsoluteSimulationFrame clientSimulationFrame)
        {
            if (connection.CanSend)
            {
                SerializeAndQueuePackets(clientSimulationFrame);

                UpdateHeldEntities();
            }
        }

        public void OnDeliveryInfo(DeliveryInfo info)
        {
            logger.Trace("OnDeliveryInfo", ("info", info.ToString()));

            foreach (var channel in channels.Values)
            {
                channel.OnDeliveryInfo(info, ref ackedEntities, ref ackedComponentsPerEntity);
            }
        }

        // Can be used for scene changes to know when all the transfers and orphaning is done.
        public bool IsEntityInAuthTransfer(Entity id)
        {
            return entitiesInAuthTransfer.Contains(id);
        }

        public bool CanSendUpdates(Entity id)
        {
            if (IsEntityInAuthTransfer(id))
            {
                return false;
            }

            return true;
        }

        public void CreateEntity(Entity id, ICoherenceComponentData[] data) => DefaultChannel.CreateEntity(id, data);

        public void UpdateEntity(Entity id, ICoherenceComponentData[] data)
        {
            if (entitiesInAuthTransfer.Contains(id))
            {
                logger.Error(Error.CoreOutConnectionUpdateOnHeldEntity, ("entity", id));
                return;
            }

            DefaultChannel.UpdateComponents(id, data);
        }

        public void RemoveComponent(Entity id, uint[] componentTypes)
        {
            if (entitiesInAuthTransfer.Contains(id))
            {
                logger.Error(Error.CoreOutConnectionRemoveComponentHeldEntity, ("entity", id));
                return;
            }

            DefaultChannel.RemoveComponents(id, componentTypes, ackedComponentsPerEntity);
        }

        public void DestroyEntity(Entity id) => DefaultChannel.DestroyEntity(id, ackedEntities);

        public void PushCommand(IEntityCommand message, MessageTarget target, Entity id, ChannelID channelID)
        {
            if (!channels.TryGetValue(channelID, out var channel))
            {
                throw new ArgumentException($"Channel with ID {channelID} doesn't exist.", nameof(channelID));
            }

            channel.PushCommand(message, target, id, connection.UseDebugStreams);
        }

        public void PushInput(IEntityInput message) => DefaultChannel.PushInput(message, connection.UseDebugStreams);

        private void SerializeAndQueuePackets(AbsoluteSimulationFrame clientSimulationFrame)
        {
            SerializerContext<IOutBitStream> serializerCtx = null;
            try
            {
                if (!channels.Values.Any(c => c.HasChanges(ackedEntities)))
                {
                    return;
                }

                var useDebugStream = connection.UseDebugStreams;

                var packet = connection.CreatePacket(true);

                PacketHeaderWriter.WriteHeader(packet.Stream, useDebugStream, clientSimulationFrame);

                var bitStream = useDebugStream
                        ? new DebugOutBitStream(new OutBitStream(packet.Stream))
                        : (IOutBitStream)new OutBitStream(packet.Stream);

                serializerCtx = new SerializerContext<IOutBitStream>(bitStream, useDebugStream, logger);

                Serialize.WriteFloatingOrigin(floatingOrigin, serializerCtx);

                serializerCtx.SetBitsRemainingInEmptyPacket(bitStream.RemainingBitCount);

                foreach (var (channelID, channel) in channels)
                {
                    if (!channel.HasChanges(ackedEntities))
                    {
                        // Clear serialization results so that MarkAsSent will enqueue an empty sentCache element.
                        channel.ClearLastSerializationResult();
                        continue;
                    }

                    var rewindPosition = bitStream.Position;

                    Serialize.WriteChannelID(channelID, serializerCtx);

                    var anythingSerialized = channel.Serialize(serializerCtx, clientSimulationFrame, entitiesInAuthTransfer.Count > 0, ackedEntities);

                    if (!anythingSerialized)
                    {
                        bitStream.Seek(rewindPosition);
                    }
                }

                Serialize.WriteEndOfChannels(serializerCtx);

                bitStream.Flush();

                var octetCount = packet.Stream.Position;

                ReliableSendToConnection(packet);

                allChannelUpdatesSent.Clear();
                var totalChanges = 0;
                foreach (var (channelID, channel) in channels)
                {
                    var updatesSent = channel.MarkAsSent(packet.SequenceId);
                    allChannelUpdatesSent.Add(channelID, updatesSent);
                    totalChanges += updatesSent?.Count ?? 0;
                }

                OnPacketSent?.Invoke(new PacketSentDebugInfo
                {
                    ChangesSentPerChannel = allChannelUpdatesSent,
                    TotalChanges = totalChanges,
                    OctetCount = octetCount,
                });
            }
            catch (Exception e)
            {
                if (serializerCtx != null && !string.IsNullOrEmpty(serializerCtx.Section))
                {
                    throw new SerializerException(serializerCtx.Section, serializerCtx.EntityId,
                        serializerCtx.ComponentId, e);
                }

                throw;
            }
        }

        private void ReliableSendToConnection(Brook.OutPacket packet)
        {
            connection.Send(packet);
        }

        public void ClearAllChangesForEntity(Entity id)
        {
            foreach (var channel in channels.Values)
            {
                channel.ClearAllChangesForEntity(id);
            }
        }

        /// <summary>
        ///     When changing state authority, we hold on to the command and cancel all other updates until all existing updates
        ///     are sent.  This prevents losing the complete state of the entity at the time of transfer.
        /// </summary>
        public void HoldChangesForEntity(Entity id)
        {
            entitiesInAuthTransfer.Add(id);
        }

        public void Reset()
        {
            foreach (var channel in channels.Values)
            {
                channel.Reset();
            }
            ackedEntities.Clear();
            ackedComponentsPerEntity.Clear();
            entitiesInAuthTransfer.Clear();
        }

        private void UpdateHeldEntities()
        {
            entitiesToRemove.Clear();

            foreach (var id in entitiesInAuthTransfer)
            {
                if (!channels.Values.Any(c => c.HasChangesForEntity(id)))
                {
                    entitiesToRemove.Add(id);
                }
            }

            foreach (var id in entitiesToRemove)
            {
                entitiesInAuthTransfer.Remove(id);

                OnAuthorityTransferred?.Invoke(id);
            }
        }

        internal void SetFloatingOrigin(Vector3d floatingOrigin)
        {
            this.floatingOrigin = floatingOrigin;
        }
    }
}
