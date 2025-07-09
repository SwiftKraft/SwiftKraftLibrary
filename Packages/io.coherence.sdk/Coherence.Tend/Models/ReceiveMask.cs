// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Models
{
    public struct ReceiveMask
    {
        public const int Range = 32;
        private const int LastRangeIndex = Range - 1;

        public ReceiveMask(uint mask)
        {
            Bits = mask;
        }

        public uint Bits
        {
            get;
        }

        public override string ToString()
        {
            return $"[Mask:{GetIntBinaryString(Bits)}]";
        }

        private static string GetIntBinaryString(uint n)
        {
            char[] b = new char[Range];

            for (int i = 0; i < Range; i++)
            {
                int pos = LastRangeIndex - i;
                b[pos] = (n & (1 << i)) != 0 ? '1' : '0';
            }
            return new string(b);
        }
    }
}
