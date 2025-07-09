// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.SimulationFrame
{
    public struct AbsoluteSimulationFrame
    {
        public const long Invalid = -1;

        public long Frame;

        public static AbsoluteSimulationFrame operator ++(AbsoluteSimulationFrame frame)
        {
            return new AbsoluteSimulationFrame() { Frame = frame.Frame + 1 };
        }

        public static implicit operator long(AbsoluteSimulationFrame frame)
        {
            return frame.Frame;
        }

        public static implicit operator AbsoluteSimulationFrame(long frame)
        {
            return new AbsoluteSimulationFrame() { Frame = frame };
        }

        public bool Equals(AbsoluteSimulationFrame other)
        {
            return Frame == other.Frame;
        }

        public override bool Equals(object obj)
        {
            return obj is AbsoluteSimulationFrame other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Frame.GetHashCode();
        }

        public override string ToString()
        {
            return $"[simframe {Frame} ({Frame & 0xff})]";
        }
    }

}
