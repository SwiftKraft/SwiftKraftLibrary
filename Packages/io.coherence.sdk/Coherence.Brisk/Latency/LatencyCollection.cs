// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk
{
    using System;
    using System.Collections.Generic;
    using Common;

    public static class SumCollection
    {
        public static ushort Sum(IList<ushort> values)
        {
            ushort result = 0;

            for (var i = 0; i < values.Count; i++)
            {
                result += values[i];
            }

            return result;
        }
    }

    public static class DeviationCollection
    {
        public static ushort Deviation(IList<ushort> values, ushort average)
        {
            ushort result = 0;

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                var diff = (ushort)Math.Abs(value - average);
                result += (ushort)(diff * diff);
            }

            return (ushort)Math.Sqrt(result / (float)values.Count);
        }
    }

    public class LatencyCollection
    {
        private readonly int size;
        private readonly int minSamplesForStability;
        private readonly int maxStableDeviation;
        private readonly List<ushort> latencies;

        public LatencyCollection(ConnectionSettings.PingSettings settings)
        {
            size = settings.MaxSamples;
            minSamplesForStability = Math.Min(settings.MinSamplesForStability, size);
            maxStableDeviation = settings.MaxStablePingDeviation;
            latencies = new List<ushort>(size);
        }

        public Ping Ping
        {
            get
            {
                var isStable = StableLatency(out var latency);
                return new Ping
                {
                    AverageLatencyMs = latency,
                    IsStable = isStable,
                    LatestLatencyMs = latencies.Count > 0 ? latencies[^1] : 0,
                };
            }
        }

        public void AddLatency(ushort latencyInMilliseconds)
        {
            if (latencies.Count >= size)
            {
                latencies.RemoveAt(0);
            }

            latencies.Add(latencyInMilliseconds);
        }

        public bool StableLatency(out ushort latency)
        {
            latency = CalculateAverageLatency();

            if (latencies.Count < minSamplesForStability)
            {
                return false;
            }

            var deviation = DeviationCollection.Deviation(latencies, latency);

            if (deviation > maxStableDeviation)
            {
                return false;
            }

            return true;
        }

        private ushort CalculateAverageLatency()
        {
            if (latencies.Count <= 0)
            {
                return 0;
            }

            var sum = SumCollection.Sum(latencies);
            return (ushort)(sum / latencies.Count);
        }
    }
}
