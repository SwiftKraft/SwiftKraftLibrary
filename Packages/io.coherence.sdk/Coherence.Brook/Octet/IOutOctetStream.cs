// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public interface IOutOctetStream : IOctetWriter
    {
        void WriteUint8(byte a);
        void WriteUint16(ushort a);
        void WriteUint32(uint a);
        void WriteUint64(ulong a);
        ArraySegment<byte> Close();
    }
}
