// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Stats
{
    using Brook;

    public class StubStats : IStats
    {
        public SimpleStats FetchSimpleInStats() => default;
        public SimpleStats FetchSimpleOutStats() => default;
        public void TrackIncomingMessages(MessageType messageType, int count = 1) { }
        public void TrackOutgoingMessages(MessageType messageType, int count = 1) { }
        public void TrackIncomingPacket(uint octetCount) { }
        public void TrackOutgoingPacket(uint octetCount) { }

        public void Flush(int stamp, double deltaTime) { }
    }
}
