// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    public struct Sample<T>
    {
        public T Value;
        public bool Stopped;
        public readonly double Time;
        public readonly double? Latency;

        public long Frame => (long)(Time * InterpolationSettings.SimulationFramesPerSecond);

        public Sample(T value, bool stopped, double time, double? latency)
        {
            Value = value;
            Stopped = stopped;
            Time = time;
            Latency = latency;
        }

        public override string ToString()
        {
            return $"{nameof(Value)}: {Value}, {nameof(Stopped)}: {Stopped} {nameof(Time)}: {Time}";
        }
    }
}
