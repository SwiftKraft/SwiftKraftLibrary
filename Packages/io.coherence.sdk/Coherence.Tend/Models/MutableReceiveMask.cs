// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Models
{
    using System;

    public struct MutableReceiveMask
    {
        private uint mask;
        private int validBitCount;

        public MutableReceiveMask(ReceiveMask receiveBits, int validBits)
        {
            mask = receiveBits.Bits;
            validBitCount = validBits;
        }

        public Bit ReadNextBit()
        {
            if (validBitCount == 0)
            {
                throw new Exception("Reading too many bits from receive mask!");
            }

            uint targetBit = (uint)(1 << validBitCount - 1);
            uint value = mask & targetBit;

            validBitCount--;
            return new Bit(value != 0);
        }
    }
}
