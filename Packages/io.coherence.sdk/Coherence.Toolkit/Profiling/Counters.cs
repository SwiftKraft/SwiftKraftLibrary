// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Profiling
{
    using Unity.Profiling;

    internal static class Counters
    {
        public static readonly ProfilerCategory Category = new ProfilerCategory("coherence");

        private const ProfilerCounterOptions FlushReset =
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush;

        public static readonly ProfilerCounterValue<long> BandwidthSent =
            new ProfilerCounterValue<long>(Category, "Bandwidth Out", ProfilerMarkerDataUnit.Bytes, FlushReset);

        public static readonly ProfilerCounterValue<long> BandwidthReceived =
            new ProfilerCounterValue<long>(Category, "Bandwidth In", ProfilerMarkerDataUnit.Bytes, FlushReset);

        public static readonly ProfilerCounterValue<long> MessagesSent =
            new ProfilerCounterValue<long>(Category, "Messages Out", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<long> MessagesReceived =
            new ProfilerCounterValue<long>(Category, "Messages In", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<long> UpdatesSent =
            new ProfilerCounterValue<long>(Category, "Updates Out", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<long> UpdatesReceived =
            new ProfilerCounterValue<long>(Category, "Updates In", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<long> CommandsSent =
            new ProfilerCounterValue<long>(Category, "Commands Out", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<long> CommandsReceived =
            new ProfilerCounterValue<long>(Category, "Commands In", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<long> InputsSent =
            new ProfilerCounterValue<long>(Category, "Inputs Out", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<long> InputsReceived =
            new ProfilerCounterValue<long>(Category, "Inputs In", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<int> PacketsSent =
            new ProfilerCounterValue<int>(Category, "Packets Out", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounterValue<int> PacketReceived =
            new ProfilerCounterValue<int>(Category, "Packets In", ProfilerMarkerDataUnit.Count, FlushReset);

        public static readonly ProfilerCounter<int> Latency = new ProfilerCounter<int>(Category, "Latency",
            ProfilerMarkerDataUnit.TimeNanoseconds);

        public static readonly ProfilerCounter<int> EntityCount = new ProfilerCounter<int>(Category, "Entities",
            ProfilerMarkerDataUnit.Count);
    }
}
