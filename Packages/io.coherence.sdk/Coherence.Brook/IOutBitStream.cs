// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public interface IOutBitStream
    {
        void WriteUint16(ushort value);
        void WriteInt16(short value);
        void WriteUint32(uint value);
        void WriteUint64(ulong value);
        void WriteUint8(byte value);
        void WriteBits(uint value, int count);
        void WriteRawBits(uint value, int count);
        void WriteSignedBits(int value, int count);
        void WriteBytesUnaligned(ReadOnlySpan<byte> bytes, int bitCount);
        void WriteFromStream(IInBitStream inBitStream, int bitCount);
        void Seek(uint newPosition);
        void Flush();

        bool IsFull
        {
            get;
        }

        uint Position
        {
            get;
        }

        uint RemainingBitCount
        {
            get;
        }

        uint OverflowBitCount
        {
            get;
        }
    }
}
