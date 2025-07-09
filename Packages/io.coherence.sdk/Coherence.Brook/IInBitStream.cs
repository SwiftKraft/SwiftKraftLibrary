// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public interface IInBitStream
    {
        ushort ReadUint16();
        short ReadInt16();
        uint ReadUint32();
        ulong ReadUint64();
        byte ReadUint8();
        uint ReadBits(int count);
        uint ReadRawBits(int count);
        int ReadSignedBits(int count);
        void ReadBytesUnaligned(Span<byte> buffer, int bitCount);
        int RemainingBits();
        bool IsEof { get; }
        int Position { get; }
    }
}
