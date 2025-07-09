// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using Brook;
    using SimulationFrame;
    using Entities;
    using Log;
    using System.Collections.Generic;
    using ProtocolDef;
    using System;
    using System.IO;
    using System.Numerics;
    using Coherence.Common;

    public static class Deserialize
    {
        // use native array instead of list in the future for performance
        public static void ReadWorldUpdate(List<IncomingEntityUpdate> updatesBuffer,
            AbsoluteSimulationFrame referenceSimulationFrame,
                                                    Vector3 floatingOriginDelta,
                                                    ISchemaSpecificComponentDeserialize componentDeserializer,
                                                    IInBitStream bitStream,
                                                    IComponentInfo definition,
                                                    Logger logger)
        {
            ushort lastIndex = 0;
            while (ReadEntity(bitStream, referenceSimulationFrame, ref lastIndex, out EntityWithMeta meta, out AbsoluteSimulationFrame entityRefSimulationFrame, logger))
            {
                var entityUpdate = IncomingEntityUpdate.New();
                if (meta.HasMeta)
                {
                    entityUpdate.Meta = meta;
                }

                if (meta.IsAlive)
                {
                    entityUpdate = UpdateComponents(componentDeserializer, entityUpdate, entityRefSimulationFrame, bitStream, definition, logger);
                }

                entityUpdate.Components.Updates.FloatingOriginDelta = floatingOriginDelta;

                logger.Trace("read entity",
                    ("entity", entityUpdate.Entity),
                    ("isAlive", meta.IsAlive),
                    ("comps", entityUpdate));

                updatesBuffer.Add(entityUpdate);
            }
        }

        public static IncomingEntityUpdate UpdateComponents(ISchemaSpecificComponentDeserialize componentDeserializer,
                                             IncomingEntityUpdate entityUpdate,
                                             AbsoluteSimulationFrame entityRefSimulationFrame,
                                             IInBitStream bitStream,
                                             IComponentInfo definition,
                                             Logger logger)
        {
            var componentCount = ReadComponentCount(bitStream);
            for (var i = 0; i < componentCount; i++)
            {
                var state = ReadComponentState(bitStream);
                var componentId = ReadComponentId(bitStream);

                switch (state)
                {
                    case ComponentState.Update:
                        {
                            ICoherenceComponentData update = componentDeserializer.ReadComponentUpdate(componentId, entityRefSimulationFrame, bitStream, logger);
                            entityUpdate.Components.UpdateComponent(ComponentChange.New(update));
                        }
                        break;
                    case ComponentState.Destruct:
                        {
                            var destroyComponentTypeId = componentId;
                            entityUpdate.Components.RemoveComponents(new uint[] { destroyComponentTypeId });
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), $"expected ComponentState {ComponentState.Update} or {ComponentState.Destruct}, received: {state}. The replication server might be outdated.");
                }
            }
            return entityUpdate;
        }

        public static bool ReadEntity(IInBitStream bitStream, AbsoluteSimulationFrame referenceSimulationFrame,
            ref ushort lastIndex, out EntityWithMeta meta, out AbsoluteSimulationFrame entityRefSimulationFrame, Logger logger)
        {
            entityRefSimulationFrame = referenceSimulationFrame;

            uint entityIndex = ReadEntityIndex(bitStream, lastIndex);
            if (entityIndex == Entity.EndOfEntities)
            {
                meta = new EntityWithMeta
                {
                    EntityId = Entity.InvalidRelative,
                    HasMeta = false,
                };
                return false;
            }

            meta = ReadEntityPotentialMeta((ushort)entityIndex, bitStream, logger);

            if (!meta.IsDestroyed)
            {
                entityRefSimulationFrame = ReadSimulationFrame(bitStream, referenceSimulationFrame);
            }

            lastIndex = (ushort)entityIndex;

            return true;
        }

        private static AbsoluteSimulationFrame ReadSimulationFrame(IInBitStream bitStream, AbsoluteSimulationFrame referenceSimulationFrame)
        {
            bool hasFrameDelta = bitStream.ReadBits(Serialize.NUM_BITS_FOR_SIMFRAME_DELTA_FLAG) != 0;
            if (!hasFrameDelta)
            {
                return referenceSimulationFrame;
            }

            short frameDelta = DeserializerTools.ReadRleSigned(bitStream);

            var isSampleStale = frameDelta == -byte.MaxValue;

            if (isSampleStale)
            {
                return AbsoluteSimulationFrame.Invalid;
            }

            return new AbsoluteSimulationFrame { Frame = referenceSimulationFrame + frameDelta };
        }

        // ReadNextMessageID reads and uses RLE decoding to get the next message id in the sequence based in the last read value.
        private static MessageID ReadNextMessageID(IInBitStream bitstream, ushort lastID)
        {
            var delta = DeserializerTools.ReadRle(bitstream);
            var sign = bitstream.ReadBits(1);
            var index = (uint)(lastID + (delta * (sign == 1 ? 1 : -1)));

            return new MessageID((ushort)index);
        }

        private static uint ReadEntityIndex(IInBitStream bitstream, ushort lastIndex)
        {
            uint delta = DeserializerTools.ReadRle(bitstream);
            if (delta == Entity.EndOfEntities)
            {
                return delta;
            }

            uint sign = bitstream.ReadBits(1);
            uint index = (uint)((uint)lastIndex + (delta * (sign == 1 ? 1 : -1)));

            return index;
        }

        private static EntityWithMeta ReadEntityPotentialMeta(ushort entityIndex, IInBitStream bitStream, Logger logger)
        {
            bool hasMeta = bitStream.ReadBits(1) != 0;
            if (hasMeta)
            {
                SerializedMeta meta = ReadEntityMeta(bitStream);

                // Potential entity construct
                Entity completeEntityId = new Entity(entityIndex, meta.Version, Entity.Relative);

                EntityWithMeta entityWithMeta = new EntityWithMeta
                {
                    EntityId = completeEntityId,
                    HasMeta = true,
                    HasStateAuthority = meta.HasStateAuthority,
                    HasInputAuthority = meta.HasInputAuthority,
                    IsOrphan = meta.IsOrphan,
                    LOD = meta.LOD,
                    Operation = meta.Operation,
                    DestroyReason = meta.DestroyReason,
                };

                return entityWithMeta;
            }

            logger.Trace("entity update");

            // Find a previous entity
            Entity entityId = default;
            EntityWithMeta entityWithoutMeta = new EntityWithMeta
            {
                EntityId = entityId,
                HasMeta = false,
                HasStateAuthority = false,
                IsOrphan = false,
                LOD = 0,
                Operation = EntityOperation.Update
            };

            return entityWithoutMeta;
        }

        private static SerializedMeta ReadEntityMeta(IInBitStream bitstream)
        {
            var version = ReadEntityVersion(bitstream);
            bool stateAuthority = ReadEntityAuthority(bitstream);
            bool inputAuthority = ReadEntityAuthority(bitstream);
            bool orphan = ReadEntityOrphan(bitstream);
            uint lod = ReadEntityLOD(bitstream);
            EntityOperation operation = ReadEntityOperation(bitstream);

            var destroyReason = DestroyReason.BadReason;
            if (operation == EntityOperation.Destroy)
            {
                destroyReason = ReadDestroyReason(bitstream);
            }

            return new SerializedMeta
            {
                Version = version,
                HasStateAuthority = stateAuthority,
                HasInputAuthority = inputAuthority,
                IsOrphan = orphan,
                LOD = lod,
                Operation = operation,
                DestroyReason = destroyReason,
            };
        }

        private static byte ReadEntityVersion(IInBitStream bitstream)
        {
            byte versionValue = (byte)bitstream.ReadBits(Entity.NumVersionBits);
            return versionValue;
        }

        private static bool ReadEntityAuthority(IInBitStream bitstream)
        {
            return bitstream.ReadBits(Serialize.NUM_BITS_FOR_AUTHORITY) != 0;
        }

        private static bool ReadEntityOrphan(IInBitStream bitstream)
        {
            return bitstream.ReadBits(Serialize.NUM_BITS_FOR_ORPHAN) != 0;
        }

        private static uint ReadEntityLOD(IInBitStream bitstream)
        {
            return (uint)bitstream.ReadBits(Serialize.NUM_BITS_FOR_LOD);
        }

        private static EntityOperation ReadEntityOperation(IInBitStream bitstream)
        {
            return (EntityOperation)bitstream.ReadBits(Serialize.NUM_BITS_FOR_OPERATION);
        }

        private static DestroyReason ReadDestroyReason(IInBitStream bitstream)
        {
            return (DestroyReason)bitstream.ReadBits(Serialize.NUM_BITS_FOR_DESTROY_REASON);
        }

        private static uint ReadComponentCount(IInBitStream bitstream)
        {
            return bitstream.ReadBits(Serialize.NUM_BITS_FOR_COMPONENT_COUNT);
        }

        private static ComponentState ReadComponentState(IInBitStream bitstream)
        {
            uint stateValue = bitstream.ReadBits(Serialize.NUM_BITS_FOR_COMPONENT_STATE);
            return (ComponentState)stateValue;
        }

        private static uint ReadComponentId(IInBitStream bitstream)
        {
            return ReadComponentTypeId(bitstream);
        }

        private static uint ReadComponentTypeId(IInBitStream bitstream)
        {
            return bitstream.ReadUint16();
        }

        public static Vector3d ReadFloatingOrigin(IInBitStream bitstream, Logger logger)
        {
            var protocolBitStream = new InProtocolBitStream(bitstream);

            var floatingOrigin = protocolBitStream.ReadVector3d();
            logger.Trace("FloatingOrigin", ("position", floatingOrigin));
            return floatingOrigin;
        }

        public static bool ReadChannelID(IInBitStream bitstream, out ChannelID channelID)
        {
            channelID = (ChannelID)bitstream.ReadBits(Serialize.NUM_BITS_FOR_CHANNEL_ID);
            return channelID != ChannelID.EndOfChannels;
        }

        public static List<(MessageID, IEntityMessage)> ReadOrderedCommands(
            IInBitStream bitStream, ISchemaSpecificComponentDeserialize componentDeserializer, Logger logger)
        {
            List<(MessageID, IEntityMessage)> res = new(32);

            ushort lastMessageID = 0;
            while (DeserializeCommands.DeserializeCommand(bitStream, out var messageType))
            {
                if (messageType != MessageType.Command)
                {
                    throw new InvalidDataException($"Unexpected message type '{messageType}'");
                }

                var id = ReadNextMessageID(bitStream, lastMessageID);
                var command = componentDeserializer.ReadCommand(bitStream, logger);

                res.Add((id, command));
                lastMessageID = id.Value;
            }

            return res;
        }
    }
}

