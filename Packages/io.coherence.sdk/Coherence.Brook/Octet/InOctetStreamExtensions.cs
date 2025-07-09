// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public static class InOctetStreamExtensions
    {
        public static ReadOnlySpan<byte> GetOffsetBuffer(this IInOctetStream stream)
        {
            return stream.GetBuffer()[(int)stream.Position..];
        }
    }
}
