// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    public struct FloatMeta
    {
        public FloatCompression Compression;
        public int BitCount;
        public int Minimum;
        public int Maximum;
        public double Precision;

        public static void EnsureValidTruncatedBitCount(int bits)
        {
            const int minBits = 10; // sign (1) + exponent (8) + at least 1 mantissa bit
            const int maxBits = 32; // full float

            if(bits < minBits) throw new ArgumentOutOfRangeException(nameof(bits), $"Number of bits must be greater than or equal to {minBits}, was {bits}");
            if(bits > maxBits) throw new ArgumentOutOfRangeException(nameof(bits), $"Number of bits must be less than or equal to {maxBits}, was {bits}");
        }

        public static FloatMeta NoCompression()
        {
            return new FloatMeta()
            {
                Compression = FloatCompression.None,
                BitCount = sizeof(float) * 8,
            };
        }

        public static FloatMeta ForTruncated(int bits)
        {
            EnsureValidTruncatedBitCount(bits);

            return new FloatMeta()
            {
                Compression = FloatCompression.Truncated,
                BitCount = bits,
            };
        }

        public static FloatMeta ForFixedPoint(int minRange, int maxRange, double precision)
        {
            return new FloatMeta()
            {
                Compression = FloatCompression.FixedPoint,
                BitCount = Utils.GetNumberOfBitsForFixedPointCompression(minRange, maxRange, precision, out _, out _),
                Minimum = minRange,
                Maximum = maxRange,
                Precision = precision
            };
        }
    }
}
