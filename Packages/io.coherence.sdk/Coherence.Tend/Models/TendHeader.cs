// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Models
{
    using Brook;

    public struct TendHeader
    {
        public bool isReliable;
        public SequenceId packetId;
        public SequenceId receivedId;
        public ReceiveMask receiveMask;

        public override string ToString()
        {
            return $"{nameof(isReliable)}: {isReliable}, {nameof(packetId)}: {packetId}, {nameof(receivedId)}: {receivedId}, {nameof(receiveMask)}: {receiveMask}";
        }
    }
}
