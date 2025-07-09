// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using System.Collections.Generic;
    using Brook;
    using Brook.Octet;
    using Coherence.Log;

    public static class MessageQueueSerializer
    {
        // MessagesQueue header
        private const uint MessageTypeBitCount = 8;
        private const uint MessageCountBitCount = 8;
        private const uint MessageHeaderBitCount = MessageTypeBitCount + MessageCountBitCount;

        // Defined by MessageCountBitCount
        private const int MaxMessageCount = 255;

        public static (int, uint) GetCountFromBudget(Queue<SerializedEntityMessage> messages, uint maxBudget, bool useDebugStreams)
        {
            if (messages.Count == 0)
            {
                return (0, 0);
            }

            // MessageQueue header = message type + message count
            var bitCount = MessageHeaderBitCount;
            if (useDebugStreams)
            {
                bitCount += (uint)DebugStreamTypes.DebugBitsSize(2); // Two writes
            }

            int messageCount = 0;

            foreach (var message in messages)
            {
                // Message header = entity ID + version + message target + componentID (+ simulation frame for inputs)
                // The header has already been pre-serialized and is accounted for in message.BitCount
                var messageBitCount = message.BitCount;
                if (bitCount + messageBitCount >= maxBudget)
                {
                    break;
                }

                bitCount += message.BitCount;
                messageCount++;
            }

            if (messageCount > MaxMessageCount)
            {
                messageCount = MaxMessageCount;
            }

            return (messageCount, bitCount);
        }

        /// <summary>
        /// Serializes messages to a bit stream up to the specified budget of <paramref name="bitBudget"/> maximum number of bits.
        /// </summary>
        public static void SerializeQueue(
            List<SerializedEntityMessage> serializedMessagesBuffer,
            MessageType messageType,
            SerializerContext<IOutBitStream> ctx,
            Queue<SerializedEntityMessage> messages,
            uint bitBudget)
        {
            if (ctx.BitStream.IsFull)
            {
                return;
            }

            (int messageCount, _) = GetCountFromBudget(messages, bitBudget, ctx.UseDebugStreams);
            if (messageCount == 0)
            {
                return;
            }

            ctx.BitStream.WriteUint8((byte)messageType);
            ctx.BitStream.WriteUint8((byte)messageCount);
            for (var i = 0; i < messageCount; ++i)
            {
                var message = messages.Dequeue();

                try
                {
                    ctx.SetEntity(message.TargetEntity);
                    ctx.BitStream.WriteBytesUnaligned(message.Octets, (int)message.BitCount);
                }
                catch
                {
                    // put the message back on the queue.
                    messages.Enqueue(message);

                    throw;
                }

                ctx.Logger.Trace("serialized message",
                    ("target", message.TargetEntity.ToString()),
                    ("type", message.GetType().ToString()));

                serializedMessagesBuffer.Add(message);
            }
        }
    }

}
