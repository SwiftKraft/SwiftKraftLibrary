// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public interface IOctetReader
    {
        byte ReadOctet();
        ReadOnlySpan<byte> ReadOctets(int octetCount);

        uint Position { get; }
        uint Length { get; }
        int RemainingOctetCount { get; }
    }
}
