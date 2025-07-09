// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;
    using Debugging;

    public class DebugInBitStream : IInBitStream
    {
        private readonly IInBitStream bitStream;

        public int Position => bitStream.Position;
        public bool IsEof => bitStream.IsEof;

        public DebugInBitStream(IInBitStream bitStream)
        {
            this.bitStream = bitStream;
        }

        private void CheckType(DebugSerializeType expectedType, int expectedBitCount)
        {
            int position = Position;

            DebugSerializeType type = (DebugSerializeType)bitStream.ReadBits(DebugStreamTypes.TypeBitCount);
            uint bitCount = bitStream.ReadBits(DebugStreamTypes.BitCountBitCount);

            if (bitCount != expectedBitCount)
            {
                throw new Exception($"Expected type {expectedType} bitcount {expectedBitCount} received type {type} {bitCount} at pos: {position}");
            }

            if (type != expectedType)
            {
                throw new Exception($"Expected type {expectedType} received {type} at pos: {position}");
            }
        }

        public void ReadBytesUnaligned(Span<byte> buffer, int bitCount)
        {
            bitStream.ReadBytesUnaligned(buffer, bitCount);
        }

        public ushort ReadUint16()
        {
            CheckType(DebugSerializeType.UnsignedBits, 16);
            return bitStream.ReadUint16();
        }

        public int ReadSignedBits(int count)
        {
            CheckType(DebugSerializeType.SignedBits, count);
            return bitStream.ReadSignedBits(count);
        }

        public int RemainingBits()
        {
            return bitStream.RemainingBits();
        }

        public short ReadInt16()
        {
            CheckType(DebugSerializeType.SignedBits, 16);
            return bitStream.ReadInt16();
        }

        public uint ReadUint32()
        {
            CheckType(DebugSerializeType.UnsignedBits, 32);
            return bitStream.ReadUint32();
        }

        public ulong ReadUint64()
        {
            CheckType(DebugSerializeType.Uint64, 64);
            return bitStream.ReadUint64();
        }

        public byte ReadUint8()
        {
            CheckType(DebugSerializeType.UnsignedBits, 8);
            return bitStream.ReadUint8();
        }

        public uint ReadBits(int count)
        {
            CheckType(DebugSerializeType.UnsignedBits, count);
            return bitStream.ReadBits(count);
        }

        public uint ReadRawBits(int count)
        {
            return bitStream.ReadBits(count);
        }
    }
}
