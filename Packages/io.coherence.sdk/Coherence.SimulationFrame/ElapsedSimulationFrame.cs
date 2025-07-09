// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.SimulationFrame
{
    public static class ElapsedSimulationFrame
    {
        public static AbsoluteSimulationFrame FromElapsedMilliseconds(long ms)
        {
            return new AbsoluteSimulationFrame { Frame = ms / (1000 / 60) };
        }
    }
}
