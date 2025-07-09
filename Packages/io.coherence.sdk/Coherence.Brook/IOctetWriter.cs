// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public interface IOctetWriter
    {
        void WriteOctet(byte v);
        void WriteOctets(byte[] v);
        void WriteOctets(ReadOnlySpan<byte> v);
        void Seek(uint newPosition);

        ReadOnlySpan<byte> Octets { get; }
        uint Position { get; }
        uint Capacity { get; }
        uint RemainingOctetCount { get; }
    }
}
