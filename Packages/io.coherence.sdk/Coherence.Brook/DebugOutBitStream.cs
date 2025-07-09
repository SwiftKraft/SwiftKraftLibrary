// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;
    using Debugging;

    public class DebugOutBitStream : IOutBitStream
    {
        private readonly IOutBitStream bitStream;

        public uint RemainingBitCount => bitStream.RemainingBitCount;
        public uint Position => bitStream.Position;
        public bool IsFull => bitStream.IsFull;
        public uint OverflowBitCount => bitStream.OverflowBitCount;

        public DebugOutBitStream(IOutBitStream bitStream)
        {
            this.bitStream = bitStream;
        }

        public void WriteUint16(ushort v)
        {
            WriteType(DebugSerializeType.UnsignedBits, 16);
            bitStream.WriteUint16(v);
        }

        public void WriteInt16(short v)
        {
            WriteType(DebugSerializeType.SignedBits, 16);
            bitStream.WriteInt16(v);
        }

        public void WriteUint32(uint v)
        {
            WriteType(DebugSerializeType.UnsignedBits, 32);
            bitStream.WriteUint32(v);
        }

        public void WriteUint64(ulong v)
        {
            WriteType(DebugSerializeType.Uint64, 64);
            bitStream.WriteUint64(v);
        }

        public void WriteUint8(byte v)
        {
            WriteType(DebugSerializeType.UnsignedBits, 8);
            bitStream.WriteUint8(v);
        }

        public void WriteBytesUnaligned(ReadOnlySpan<byte> bytes, int bitCount)
        {
            bitStream.WriteBytesUnaligned(bytes, bitCount);
        }

        public void WriteFromStream(IInBitStream inBitStream, int bitCount)
        {
            bitStream.WriteFromStream(inBitStream, bitCount);
        }

        public void Seek(uint newPosition)
        {
            bitStream.Seek(newPosition);
        }

        public void WriteSignedBits(int v, int count)
        {
            WriteType(DebugSerializeType.SignedBits, count);
            bitStream.WriteSignedBits(v, count);
        }

        public void WriteBits(uint v, int count)
        {
            WriteType(DebugSerializeType.UnsignedBits, count);
            bitStream.WriteBits(v, count);
        }

        private void WriteType(DebugSerializeType type, int bitCount)
        {
            InternalWriteBits((uint)type, DebugStreamTypes.TypeBitCount);
            InternalWriteBits((uint)bitCount, DebugStreamTypes.BitCountBitCount);
        }

        private void InternalWriteBits(uint v, int count)
        {
            bitStream.WriteBits(v, count);
        }

        public void WriteRawBits(uint v, int count)
        {
            InternalWriteBits(v, count);
        }

        public void Flush()
        {
            bitStream.Flush();
        }
    }
}
