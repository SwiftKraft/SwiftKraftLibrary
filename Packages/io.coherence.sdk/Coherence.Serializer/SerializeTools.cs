// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using Brook;
    using Coherence.ProtocolDef;
    using Entities;

    public static class SerializeTools
    {
        public static void WriteRleSigned(IOutBitStream stream, short v)
        {
            uint signBit = v < 0 ? 1u : 0u;
            stream.WriteBits(signBit, 1);
            WriteRle(stream, (uint)(signBit == 0 ? v : -v));
        }

        public static void WriteRle(IOutBitStream stream, uint v)
        {
            if (v <= 7)
            {
                stream.WriteBits(0, 1);
                stream.WriteBits(v, 3);
                return;
            }

            stream.WriteBits(1, 1);

            if (v <= 255)
            {
                stream.WriteBits(0, 1);
                stream.WriteBits(v, 8);
                return;
            }

            stream.WriteBits(1, 1);

            if (v < 65535)
            {
                stream.WriteBits(0, 1);
                stream.WriteBits(v, 16);
                return;
            }

            stream.WriteBits(1, 1);
        }

        public static void SerializeEntity(Entity entity, IOutBitStream outBitStream)
        {
            entity.AssertRelative();

            outBitStream.WriteBits((uint)entity.Index, Entity.NumIndexBits);
            outBitStream.WriteBits((uint)entity.Version, Entity.NumVersionBits);
        }

        public static void SerializeComponentTypeID(uint componentTypeId, IOutBitStream outBitStream)
        {
            outBitStream.WriteUint16((byte)componentTypeId);
        }

        public static void WriteFieldSimFrameDelta(IOutProtocolBitStream stream, byte delta)
        {
            if (delta == 0)
            {
                stream.WriteBits(0, 1);
                return;
            }

            stream.WriteBits(1, 1);

            if (delta < 1 << 2)
            {
                stream.WriteBits(0, 1);
                stream.WriteBits(delta, 2);
                return;
            }

            stream.WriteBits(1, 1);

            if (delta < 1 << 4)
            {
                stream.WriteBits(0, 1);
                stream.WriteBits(delta, 4);
                return;
            }

            stream.WriteBits(1, 1);
            stream.WriteBits(delta, 8);
        }
    }
}
