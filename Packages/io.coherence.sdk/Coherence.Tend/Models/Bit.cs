// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Models
{
    public struct Bit
    {
        public Bit(bool on)
        {
            IsOn = on;
        }

        public bool IsOn { get; }
    }
}
