// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public interface IInOctetStream : IOctetReader
    {
        ushort ReadUint16();
        uint ReadUint32();
        ulong ReadUint64();
        byte ReadUint8();
        ReadOnlySpan<byte> GetBuffer();
    }
}
