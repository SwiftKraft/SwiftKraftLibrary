// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Stats
{
    using System;

    public struct SimpleStats
    {
        public int PacketCount;
        public long OctetTotalSize;
        public int MessageCount;
        public int ChangeCount;
        public int CommandCount;
        public int InputCount;
        public TimeSpan Duration;
        public int Stamp;
    }

    public class SimpleStatsCollector
    {
        private SimpleStats currentStats;
        private readonly object lockObject = new object();

        public void TrackPacket(int octetCount)
        {
            lock (lockObject)
            {
                currentStats.PacketCount++;
                currentStats.OctetTotalSize += octetCount;
            }
        }

        public void TrackMessage(MessageType messageType, int count)
        {
            lock (lockObject)
            {
                currentStats.MessageCount += count;
                switch (messageType)
                {
                    case MessageType.EcsWorldUpdate:
                        currentStats.ChangeCount += count;
                        break;
                    case MessageType.Command:
                        currentStats.CommandCount += count;
                        break;
                    case MessageType.Input:
                        currentStats.InputCount += count;
                        break;
                }
            }
        }

        public SimpleStats GetStatsAndClear(int stamp, TimeSpan duration)
        {
            lock (lockObject)
            {
                currentStats.Stamp = stamp;
                currentStats.Duration = duration;
                var finalStats = currentStats;
                currentStats = default;

                return finalStats;
            }
        }
    }
}
