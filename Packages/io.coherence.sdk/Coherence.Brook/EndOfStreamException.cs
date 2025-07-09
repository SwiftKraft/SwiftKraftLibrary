// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public class EndOfStreamException : Exception
    {
        public EndOfStreamException()
        {
        }

        public EndOfStreamException(int requestedRead, int remainingBits) : base($"requested:{requestedRead} remaining:{remainingBits}")
        {
        }

        public EndOfStreamException(string message) : base(message)
        {
        }

        public EndOfStreamException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
