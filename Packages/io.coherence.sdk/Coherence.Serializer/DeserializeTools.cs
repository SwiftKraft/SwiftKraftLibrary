// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using Brook;
    using Coherence.ProtocolDef;
    using Entities;

    public static class DeserializerTools
    {
        public static short ReadRleSigned(IInBitStream stream)
        {
            bool hasSign = stream.ReadBits(1) != 0;
            uint value = ReadRle(stream);
            return hasSign ? (short)(value * -1) : (short)value;
        }

        public static uint ReadRle(IInBitStream stream)
        {
            uint b = stream.ReadBits(1);
            if (b == 0)
            {
                return stream.ReadBits(3);
            }

            uint c = stream.ReadBits(1);
            if (c == 0)
            {
                return stream.ReadBits(8);
            }

            uint d = stream.ReadBits(1);
            return d == 0 ? stream.ReadBits(16) : 65535u;
        }

        public static Entity DeserializeEntity(IInBitStream outBitStream)
        {
            ushort rawIndex = (ushort)outBitStream.ReadBits(Entity.NumIndexBits);
            byte rawVersion = (byte)outBitStream.ReadBits(Entity.NumVersionBits);

            Entity entityId = new Entity(rawIndex, rawVersion, Entity.Relative);

            return entityId;
        }

        public static uint DeserializeComponentTypeID(IInBitStream inBitStream)
        {
            return (uint)inBitStream.ReadUint16();
        }

        public static MessageTarget DeserializeMessageTarget(IInBitStream inBitStream)
        {
            return (MessageTarget)inBitStream.ReadBits(2);
        }

        public static byte ReadFieldSimFrameDelta(IInProtocolBitStream stream)
        {
            if (stream.ReadBits(1) == 0)
            {
                return 0;
            }

            if (stream.ReadBits(1) == 0)
            {
                return (byte)stream.ReadBits(2);
            }

            if (stream.ReadBits(1) == 0)
            {
                return (byte)stream.ReadBits(4);
            }

            return (byte)stream.ReadBits(8);
        }
    }
}
