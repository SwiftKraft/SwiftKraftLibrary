// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using Entities;
    using System.Collections.Generic;
    using Brook;
    using ProtocolDef;
    using System;
    using Brook.Octet;
    using Log;
    using SimulationFrame;
    using Coherence.Common;
    using Version = ProtocolDef.Version;

    public static class Serialize
    {
        private static readonly uint NUM_BITS_FOR_END_OF_ENTITIES = 3;
        private static readonly uint DEBUG_NUM_BITS_FOR_END_OF_ENTITIES = NUM_BITS_FOR_END_OF_ENTITIES + (uint)Brook.DebugStreamTypes.DebugBitsSize(3); //three writes
        public const uint NUM_BITS_FOR_MESSAGE_TYPE = 8;
        public const int NUM_BITS_FOR_DESTROY_REASON = 3;
        public const int NUM_BITS_FOR_SIMFRAME_DELTA_FLAG = 1;
        public const int MAX_SERIALIZED_MESSAGE_BYTES = 1024;
        public const int NUM_BITS_FOR_AUTHORITY = 1;
        public const int NUM_BITS_FOR_ORPHAN = 1;
        public const int NUM_BITS_FOR_OPERATION = 2;
        public const int NUM_BITS_FOR_LOD = 4;
        public const int NUM_BITS_FOR_COMPONENT_COUNT = 5;
        public const int NUM_BITS_FOR_COMPONENT_STATE = 2;
        public const int NUM_BITS_FOR_MESSAGE_TARGET = 2;
        public const int NUM_BITS_FOR_CHANNEL_ID = 4;

        private static uint ChannelIDBits(SerializerContext<IOutBitStream> ctx)
        {
            var numBitsForChannelID = ctx.UseDebugStreams
                ? NUM_BITS_FOR_CHANNEL_ID + (uint)DebugStreamTypes.DebugBitsSize(1)
                : NUM_BITS_FOR_CHANNEL_ID;
            return ctx.ProtocolVersion >= Version.VersionIncludesChannelID ? numBitsForChannelID : 0;
        }

        private static uint MessageTypeBits(SerializerContext<IOutBitStream> ctx)
        {
            return ctx.UseDebugStreams
                ? NUM_BITS_FOR_MESSAGE_TYPE + (uint)DebugStreamTypes.DebugBitsSize(1)
                : NUM_BITS_FOR_MESSAGE_TYPE;
        }

        private static uint EndOfPacketBits(SerializerContext<IOutBitStream> ctx)
        {
            var numBitsForEndOfMessages = MessageTypeBits(ctx);
            var numBitsForEndOfChannels = ChannelIDBits(ctx);

            return ctx.ProtocolVersion switch
            {
                >= Version.VersionIncludesChannelID => numBitsForEndOfMessages + numBitsForEndOfChannels,
                >= Version.VersionIncludesEndOfMessagesMarker => numBitsForEndOfMessages,
                _ => 0,
            };
        }

        private static bool HasEnoughBits(SerializerContext<IOutBitStream> ctx, uint count)
        {
            if (ctx.BitStream.IsFull)
            {
                return false;
            }

            return ctx.BitStream.RemainingBitCount >= (count + EndOfPacketBits(ctx));
        }

        private static uint RemainingBitBudget(SerializerContext<IOutBitStream> ctx)
        {
            var endOfPacketBits = EndOfPacketBits(ctx);
            return endOfPacketBits < ctx.BitStream.RemainingBitCount
                ? ctx.BitStream.RemainingBitCount - endOfPacketBits
                : 0;
        }

        public static void WriteEntityUpdates(
            List<Entity> writtenEntitiesBuffer,
            IReadOnlyList<EntityChange> changes,
            AbsoluteSimulationFrame referenceSimulationFrame,
            ISchemaSpecificComponentSerialize componentSerializer,
            SerializerContext<IOutBitStream> ctx)
        {
            if (changes.Count == 0)
            {
                return;
            }

            uint endOfEntitiesSize = NUM_BITS_FOR_END_OF_ENTITIES;
            if (ctx.UseDebugStreams)
            {
                endOfEntitiesSize = DEBUG_NUM_BITS_FOR_END_OF_ENTITIES;
            }

            var rewindPoint = ctx.BitStream.Position;

            ctx.StartSection(nameof(MessageType.EcsWorldUpdate));
            SerializeCommand(MessageType.EcsWorldUpdate, ctx.BitStream);

            // make sure we have at least enough to write an empty entity update.
            if (!HasEnoughBits(ctx, endOfEntitiesSize))
            {
                ctx.BitStream.Seek(rewindPoint);
                return;
            }

            ushort lastIndex = 0;

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                ctx.SetEntity(change.ID);

                rewindPoint = ctx.BitStream.Position;
                uint bitsTaken = 0;

                if (change.Update.IsDestroy)
                {
                    SerializeDestroyed(change, ctx, ref lastIndex);
                }
                else
                {
                    SerializeUpdated(change, referenceSimulationFrame, componentSerializer, ctx, ref lastIndex,
                        out bitsTaken);
                }

                if (!HasEnoughBits(ctx, endOfEntitiesSize))
                {
                    ctx.BitStream.Seek(rewindPoint);

                    var bitNeededToClosePacket = EndOfPacketBits(ctx) + endOfEntitiesSize;
                    var maxSizeBits = ctx.BitsRemainingInEmptyPacket - bitNeededToClosePacket;
                    if (bitsTaken > maxSizeBits)
                    {
                        ctx.Logger.Warning(Warning.SerializeTooBig,
                            ("entity", change.ID),
                            ("entitySizeBits", bitsTaken),
                            ("maxSizeBits", maxSizeBits));
                    }

                    break;
                }

                writtenEntitiesBuffer.Add(change.ID);
            }

            WriteEndOfEntities(ctx.BitStream);

            ctx.EndSection();
        }

        private static bool SerializeSimulationFrame(AbsoluteSimulationFrame referenceSimulationFrame,
            IOutBitStream bitStream,
            Logger logger,
            AbsoluteSimulationFrame simulationFrame)
        {
            var isValid = true;

            AbsoluteSimulationFrame frameDelta = 0;

            if (simulationFrame != 0)
            {
                frameDelta = simulationFrame - referenceSimulationFrame;
                if (frameDelta > byte.MaxValue)
                {
                    frameDelta = byte.MaxValue;
                }
                else if (frameDelta < -byte.MaxValue)
                {
                    frameDelta = -byte.MaxValue;
                    isValid = false;
                }
            }

            if (frameDelta == 0)
            {
                bitStream.WriteBits(0, NUM_BITS_FOR_SIMFRAME_DELTA_FLAG);
            }
            else
            {
                bitStream.WriteBits(1, NUM_BITS_FOR_SIMFRAME_DELTA_FLAG);
                SerializeTools.WriteRleSigned(bitStream, (short)frameDelta);
            }

            return isValid;
        }

        // This one does more writes so it earlies out if it can't write the whole thing.
        public static void SerializeUpdated(EntityChange change,
            AbsoluteSimulationFrame referenceSimulationFrame,
            ISchemaSpecificComponentSerialize componentSerializer,
            SerializerContext<IOutBitStream> ctx,
            ref ushort lastIndex,
            out uint bitsTaken)
        {
            if (change.Update.Operation == EntityOperation.Unknown)
            {
                ctx.Logger.Error(Error.SerializeInvalidEntityOperation,
                    ("entity", change.ID),
                    ("operation", change.Update.Operation));
                bitsTaken = 0;
                return;
            }


            var initialRemainingBitCount = ctx.BitStream.RemainingBitCount;
            var initialBufferPosition = ctx.BitStream.Position;

            lastIndex = WriteEntityIndex(change.ID, ctx.BitStream, lastIndex);

            WriteEntityMeta(change.Meta, ctx.BitStream);

            var entityRefSimulationFrame = GetMinSimFrame(change, ctx.Logger) ?? referenceSimulationFrame;
            var isRefSimFrameValid = SerializeSimulationFrame(referenceSimulationFrame, ctx.BitStream, ctx.Logger, entityRefSimulationFrame);

            WriteComponentCount(change.Update.Components.Count, ctx.BitStream);

            foreach (var kvp in change.Update.Components.Updates.Store)
            {
                const ComponentState state = ComponentState.Update;
                uint baseType = kvp.Key;
                uint lodedType = kvp.Value.ComponentSerializeType;
                ctx.SetComponent(lodedType);

                WriteComponentState(state, ctx.BitStream);
                WriteComponentId(lodedType, ctx.BitStream);

                var protocolStream = OutProtocolBitStream.Shared.Reset(ctx.BitStream, ctx.Logger);
                uint leftoverMask = componentSerializer.WriteComponentUpdate(kvp.Value.Data, lodedType, isRefSimFrameValid, entityRefSimulationFrame, protocolStream, ctx.Logger);
                if (leftoverMask != 0)
                {
                    // Changed to debug since we know this is possible but we should only see it when we're tracking down
                    // issues.
                    ctx.Logger.Debug("After serializing a component, it's mask wasn't fully consumed.",
                        ("entity", change.ID),
                        ("componentType", baseType),
                        ("lodedType", lodedType),
                        ("originalMask", kvp.Value.Data.FieldsMask),
                        ("leftoverMask", leftoverMask));
                }
            }

            foreach (var componentType in change.Update.Components.Destroys)
            {
                const ComponentState state = ComponentState.Destruct;
                ctx.SetComponent(componentType);

                WriteComponentState(state, ctx.BitStream);
                WriteComponentId(componentType, ctx.BitStream);
            }

            if (ctx.BitStream.IsFull)
            {
                bitsTaken = initialRemainingBitCount + ctx.BitStream.OverflowBitCount;
            }
            else
            {
                bitsTaken = ctx.BitStream.Position - initialBufferPosition;
            }

            if (change.Update.IsUpdate)
            {
                ctx.Logger.Trace(
                    "SerializeUpdated",
                    ("entity", change.ID),
                    ("operation", change.Update.Operation),
                    ("comps", change.Update.Components));
            }
            else
            {
                ctx.Logger.Debug(
                    "SerializeUpdated",
                    ("entity", change.ID),
                    ("operation", change.Update.Operation),
                    ("comps", change.Update.Components));
            }
        }

        private static void SerializeDestroyed(EntityChange change,
            SerializerContext<IOutBitStream> ctx, ref ushort lastIndex)
        {
            if (change.Update.Operation == EntityOperation.Unknown)
            {
                ctx.Logger.Error(Error.SerializeInvalidEntityOperation,
                    ("entity", change.ID),
                    ("operation", change.Update.Operation));
                return;
            }

            lastIndex = WriteEntityIndex(change.ID, ctx.BitStream, lastIndex);

            WriteEntityMeta(change.Meta, ctx.BitStream);

            WriteEntityDestroyReason(change.Meta.DestroyReason, ctx.BitStream);

            ctx.Logger.Debug("SerializeDestroyed", ("entity", change.ID));
        }

        private static void SerializeCommand(MessageType messageType, IOutBitStream outBitStream)
        {
            byte messageOctet = (byte)messageType;
            outBitStream.WriteUint8(messageOctet);
        }

        private static void WriteMessageIDDelta(ushort id, ushort lastId, IOutBitStream stream)
        {
            var delta = id - lastId;
            SerializeTools.WriteRle(stream, (uint)Math.Abs(delta));
            stream.WriteBits(delta < 0 ? 0u : 1u, 1);
        }

        private static ushort WriteEntityIndex(Entity entityId, IOutBitStream stream, ushort lastIndex)
        {
            int delta = entityId.Index - lastIndex;

            WriteEntityIndexDelta((int)delta, stream);
            return (ushort)entityId.Index;
        }

        private static void WriteEntityIndexDelta(int delta, IOutBitStream stream)
        {
            SerializeTools.WriteRle(stream, (uint)Math.Abs(delta));
            stream.WriteBits(delta < 0 ? 0u : 1u, 1);
        }

        private static void WriteEntityMeta(SerializedMeta entityMeta, IOutBitStream stream)
        {
            const bool shouldWriteMeta = true;    // We currently always write meta
            stream.WriteBits(shouldWriteMeta ? 1u : 0u, 1);
            if (shouldWriteMeta)
            {
                WriteEntityVersion(entityMeta.Version, stream);
                WriteEntityAuthority(entityMeta.HasStateAuthority, stream);
                WriteEntityAuthority(entityMeta.HasInputAuthority, stream);
                WriteEntityOrphan(entityMeta.IsOrphan, stream);
                WriteEntityLOD(entityMeta.LOD, stream);
                WriteEntityOperation(entityMeta.Operation, stream);
            }
        }

        private static void WriteEntityDestroyReason(DestroyReason reason, IOutBitStream stream)
        {
            stream.WriteBits((uint)reason, NUM_BITS_FOR_DESTROY_REASON);
        }

        private static void WriteEntityAuthority(bool hasAuthority, IOutBitStream stream)
        {
            stream.WriteBits(hasAuthority ? 1u : 0u, NUM_BITS_FOR_AUTHORITY);
        }

        private static void WriteEntityOrphan(bool isOrphan, IOutBitStream stream)
        {
            stream.WriteBits(isOrphan ? 1u : 0u, NUM_BITS_FOR_ORPHAN);
        }

        private static void WriteEntityLOD(uint lod, IOutBitStream stream)
        {
            stream.WriteBits(lod, NUM_BITS_FOR_LOD);
        }

        private static void WriteEntityOperation(EntityOperation operation, IOutBitStream stream)
        {
            stream.WriteBits((uint)operation, NUM_BITS_FOR_OPERATION);
        }

        private static void WriteEntityVersion(uint version, IOutBitStream stream)
        {
            uint serializeVersion = version % Entity.MaxVersions;
            stream.WriteBits(serializeVersion, Entity.NumVersionBits);
        }

        private static void WriteEndOfEntities(IOutBitStream stream)
        {
            SerializeTools.WriteRle(stream, Entity.EndOfEntities);
        }

        private static void WriteComponentCount(int count, IOutBitStream stream)
        {
            stream.WriteBits((uint)count, NUM_BITS_FOR_COMPONENT_COUNT);
        }

        private static void WriteComponentState(ComponentState state, IOutBitStream stream)
        {
            stream.WriteBits((uint)state, NUM_BITS_FOR_COMPONENT_STATE);
        }

        private static void WriteComponentId(uint componentSerializeId, IOutBitStream stream)
        {
            stream.WriteUint16((ushort)componentSerializeId);
        }

        private static void WriteMessageEntityId(Entity entityID, IOutBitStream outBitStream)
        {
            entityID.AssertRelative();

            SerializeTools.SerializeEntity(entityID, outBitStream);
        }

        private static void WriteMessageTarget(MessageTarget target, IOutBitStream outBitStream)
        {
            outBitStream.WriteBits((uint)target, NUM_BITS_FOR_MESSAGE_TARGET);
        }

        public static void WriteMessages(
            List<SerializedEntityMessage> serializedMessagesBuffer,
            MessageType messageType,
            Queue<SerializedEntityMessage> messages,
            SerializerContext<IOutBitStream> ctx)
        {
            ctx.StartSection(messageType.AsString());
            var bitBudget = RemainingBitBudget(ctx);
            MessageQueueSerializer.SerializeQueue(serializedMessagesBuffer, messageType, ctx, messages, bitBudget);
            ctx.EndSection();
        }

        public static List<MessageID> WriteOrderedCommands(List<(MessageID, SerializedEntityMessage)> messages, SerializerContext<IOutBitStream> ctx)
        {
            ctx.StartSection("OrderedCommands");

            List<MessageID> res = new(32);
            ushort lastMessageID = 0;

            foreach (var (id, message) in messages)
            {
                var rewindPos = ctx.BitStream.Position;

                ctx.BitStream.WriteUint8((byte)MessageType.Command);
                WriteMessageIDDelta(id.Value, lastMessageID, ctx.BitStream);
                lastMessageID = id.Value;

                ctx.SetEntity(message.TargetEntity);
                ctx.BitStream.WriteBytesUnaligned(message.Octets, (int)message.BitCount);

                // Ensure there are enough bits left to write both EndOfMessages and EndOfChannels
                if (!ctx.BitStream.IsFull && ctx.BitStream.RemainingBitCount >= EndOfPacketBits(ctx))
                {
                    res.Add(id);
                }
                else
                {
                    ctx.BitStream.Seek(rewindPos);
                    break;
                }
            }

            ctx.EndSection();

            return res;
        }

        public static SerializedEntityMessage SerializeMessage(MessageType messageType,
            MessageTarget target,
            IEntityMessage message,
            Entity id,
            ISchemaSpecificComponentSerialize serializer,
            bool useDebugStream,
            Logger logger)
        {
            var buffer = new byte[MAX_SERIALIZED_MESSAGE_BYTES];
            var octetStream = new OutOctetStream(buffer);
            IOutBitStream bitStream = new OutBitStream(octetStream);
            if (useDebugStream)
            {
                bitStream = new DebugOutBitStream(bitStream);
            }

            WriteMessageEntityId(id, bitStream);
            WriteMessageTarget(target, bitStream);
            WriteComponentId(message.GetComponentType(), bitStream);

            var fieldStream = OutProtocolBitStream.Shared.Reset(bitStream, logger);
            switch (messageType)
            {
                case MessageType.Command:
                    serializer.WriteCommand((IEntityCommand)message, message.GetComponentType(), fieldStream, logger);
                    break;
                case MessageType.Input:
                    serializer.WriteInput((IEntityInput)message, message.GetComponentType(), fieldStream, logger);
                    break;
                default:
                    throw new Exception($"Invalid message type: {messageType}");
            }

            if (bitStream.IsFull)
            {
                throw new Exception($"{messageType} is too large and will not be sent to {target}.");
            }

            bitStream.Flush();

            return new SerializedEntityMessage(id, buffer, bitStream.Position);
        }

        public static void WriteFloatingOrigin(Vector3d floatingOrigin, SerializerContext<IOutBitStream> ctx)
        {
            ctx.Logger.Trace("WriteFloatingOrigin", ("origin", floatingOrigin.ToString()));

            ctx.StartSection("FloatOrigin");
            var protocolBitStream = OutProtocolBitStream.Shared.Reset(ctx.BitStream, ctx.Logger);

            protocolBitStream.WriteVector3d(floatingOrigin);
            ctx.EndSection();
        }

        private static AbsoluteSimulationFrame? GetMinSimFrame(EntityChange change, Logger logger)
        {
            AbsoluteSimulationFrame? min = null;

            foreach (var component in change.Update.Components.Updates.Store)
            {
                var result = component.Value.Data.GetMinSimulationFrame();

                if (result == 0)
                {
                    logger.Error(Error.SerializeSimulationFrameZero, ("component", component.Value.Data));
                }

                if (result == null)
                {
                    continue;
                }

                if (min == null || result < min)
                {
                    min = result;
                }
            }

            return min;
        }

        public static void WriteChannelID(ChannelID channelID, SerializerContext<IOutBitStream> ctx)
        {
            if (ctx.ProtocolVersion >= Version.VersionIncludesChannelID)
            {
                if (!channelID.IsValid())
                {
                    throw new Exception($"Invalid ChannelID {channelID}, only channels {ChannelID.MinValue}-{ChannelID.MaxValue} are supported");
                }

                ctx.BitStream.WriteBits((byte)channelID, NUM_BITS_FOR_CHANNEL_ID);
                if (ctx.BitStream.IsFull && ctx.BitStream.OverflowBitCount > 0)
                {
                    throw new Exception("Failed to write ChannelID, not enough space left");
                }
            }
        }

        public static void WriteEndOfMessages(SerializerContext<IOutBitStream> ctx)
        {
            // if (ctx.ProtocolVersion >= Version.VersionIncludesEndOfMessagesMarker)
            SerializeCommand(MessageType.EndOfMessages, ctx.BitStream);
            if (ctx.BitStream.IsFull && ctx.BitStream.OverflowBitCount > 0)
            {
                throw new Exception("Failed to write EndOfMessages, not enough space left for marker");
            }
        }

        public static void WriteEndOfChannels(SerializerContext<IOutBitStream> ctx)
        {
            if (ctx.ProtocolVersion >= Version.VersionIncludesChannelID)
            {
                ctx.BitStream.WriteBits((byte)ChannelID.EndOfChannels, NUM_BITS_FOR_CHANNEL_ID);
                if (ctx.BitStream.IsFull && ctx.BitStream.OverflowBitCount > 0)
                {
                    throw new Exception("Failed to write EndOfChannels, not enough space left");
                }
            }
        }
    }
}
