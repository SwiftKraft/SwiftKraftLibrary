// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Stats
{
    using Brook;
    using System;

    public interface IStats
    {
        public SimpleStats FetchSimpleInStats();
        public SimpleStats FetchSimpleOutStats();
        public void TrackIncomingMessages(MessageType messageType, int count = 1);
        public void TrackOutgoingMessages(MessageType messageType, int count = 1);
        public void TrackIncomingPacket(uint octetCount);
        public void TrackOutgoingPacket(uint octetCount);
        public void Flush(int stamp, double deltaTime);
    }
}
