// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Stats
{
    using Brook;
    using System;

    public class Stats : IStats
    {
        private readonly SimpleStatsCollector simpleIn = new SimpleStatsCollector();
        private readonly SimpleStatsCollector simpleOut = new SimpleStatsCollector();
        private SimpleStats simpleInStats;
        private SimpleStats simpleOutStats;
        private int lastStamp;

        public SimpleStats FetchSimpleInStats()
        {
            return simpleInStats;
        }

        public SimpleStats FetchSimpleOutStats()
        {
            return simpleOutStats;
        }

        public void TrackIncomingMessages(MessageType messageType, int count = 1)
        {
            simpleIn.TrackMessage(messageType, count);
        }

        public void TrackOutgoingMessages(MessageType messageType, int count = 1)
        {
            simpleOut.TrackMessage(messageType, count);
        }

        public void TrackIncomingPacket(uint octetCount)
        {
            simpleIn.TrackPacket((int)octetCount);
        }

        public void TrackOutgoingPacket(uint octetCount)
        {
            simpleOut.TrackPacket((int)octetCount);
        }

        public void Flush(int stamp, double deltaTime)
        {
            if (lastStamp == stamp)
            {
                return;
            }

            lastStamp = stamp;
            TimeSpan duration = TimeSpan.FromSeconds(deltaTime);

            // Scrape data and reset
            simpleInStats = simpleIn.GetStatsAndClear(stamp, duration);
            simpleOutStats = simpleOut.GetStatsAndClear(stamp, duration);
        }
    }
}
