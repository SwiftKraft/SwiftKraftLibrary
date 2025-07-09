// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System.Net;

    public struct InPacket
    {
        public readonly IInOctetStream Stream;
        public readonly SequenceId SequenceId;
        public readonly bool IsReliable;
        public readonly bool IsOob;
        public readonly IPEndPoint From;

        public InPacket(IInOctetStream stream, SequenceId sequenceId, bool isReliable, bool isOob, IPEndPoint from)
        {
            Stream = stream;
            SequenceId = sequenceId;
            IsReliable = isReliable;
            IsOob = isOob;
            From = from;
        }
    }
}
