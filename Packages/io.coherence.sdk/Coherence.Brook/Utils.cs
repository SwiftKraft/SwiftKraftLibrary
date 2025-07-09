// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    internal static class Utils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint TruncateFloat(float value, int bits)
        {
            if (bits < 10 || bits > 32)
                throw new ArgumentOutOfRangeException(nameof(bits), bits, "Truncating float is possible with bit range [10, 32].");

            var truncBits = 32 - bits;
            uint mask = (uint)(-(1 << (truncBits)));
            return ((*(uint*)&value) & mask) >> truncBits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong TruncateDouble(double value, int bits)
        {
            if (bits < 13 || bits > 64)
                throw new ArgumentOutOfRangeException(nameof(bits), bits, "Truncating double is possible with bit range [13, 64].");

            ulong mask = (ulong)(-(1L << (64 - bits)));
            return (*(ulong*)&value) & mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float GetTruncatedFloatValue(float value, int bits)
        {
            var truncatedAsInt = TruncateFloat(value, bits);
            return *(float*)&truncatedAsInt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe double GetTruncatedDoubleValue(double value, int bits)
        {
            var truncatedAsInt = TruncateDouble(value, bits);
            return *(float*)&truncatedAsInt;
        }

        // Courtesy of: https://stackoverflow.com/a/31377558/3179656
        /// <summary>
        /// Calculates the position of the first bit set to 1 from the most significant bit side.
        /// Position is counted from the right (least significant bit side).
        /// </summary>
        /// <example>0b0100 returns 3</example>
        /// <example>0b0000 return 0</example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountMSBitPosition(uint input)
        {
            const int bits = 32;

            if (input == 0) return 0;

            int n = 1;
            if ((input >> (bits - 16)) == 0) { n += 16; input <<= 16; }
            if ((input >> (bits - 8)) == 0) { n += 8; input <<= 8; }
            if ((input >> (bits - 4)) == 0) { n += 4; input <<= 4; }
            if ((input >> (bits - 2)) == 0) { n += 2; input <<= 2; }
            return bits - (n - (int)(input >> (bits - 1)));
        }

        // Courtesy of: https://stackoverflow.com/a/31377558/3179656
        /// <summary>
        /// Calculates the position of the first bit set to 1 from the most significant bit side.
        /// Position is counted from the right (least significant bit side).
        /// </summary>
        /// <example>0b0100 returns 3</example>
        /// <example>0b0000 return 0</example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountMSBitPosition(ulong input)
        {
            const int bits = 64;

            if (input == 0) return 0;

            int n = 1;
            if ((input >> (bits - 32)) == 0) { n += 32; input <<= 32; }
            if ((input >> (bits - 16)) == 0) { n += 16; input <<= 16; }
            if ((input >> (bits - 8)) == 0) { n += 8; input <<= 8; }
            if ((input >> (bits - 4)) == 0) { n += 4; input <<= 4; }
            if ((input >> (bits - 2)) == 0) { n += 2; input <<= 2; }
            return bits - (n - (int)(input >> (bits - 1)));
        }

        public static uint GetMaxCursorForFixedPointCompression(long minRange, long maxRange, double precision, out long range)
        {
            range = maxRange - minRange;
            if (range <= 0) throw new ArgumentOutOfRangeException(nameof(range), $"Range must be greater than 0, was {range}");

            var maxCursorD = range / precision;
            uint maxCursor = (uint)Math.Round(maxCursorD);
            if (maxCursorD < uint.MinValue || maxCursorD > uint.MaxValue || maxCursor == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(range), $"Too high precision ({precision}) or too high ({range}) to be represented within 32 bits");
            }

            return maxCursor;
        }

        public static int GetNumberOfBitsForFixedPointCompression(long minRange, long maxRange, double precision, out long range, out uint maxCursor)
        {
            maxCursor = GetMaxCursorForFixedPointCompression(minRange, maxRange, precision, out range);
            return Utils.CountMSBitPosition(maxCursor);
        }

        public static uint CalculateCursorForFixedFloatCompression(double value, long minRange, long maxRange, double precision, out int bits)
        {
            bits = GetNumberOfBitsForFixedPointCompression(minRange, maxRange, precision, out long range, out uint maxSerializedValue);

            bool hasMinimumAllowedValue = value < minRange;
            if (hasMinimumAllowedValue)
            {
                return 0;
            }

            bool hasMaximumAllowedValue = value > minRange + range;
            if (hasMaximumAllowedValue)
            {
                return maxSerializedValue;
            }

            double offsetValue = value - minRange;

            uint cursor = (uint)Math.Round(((offsetValue) / range) * maxSerializedValue, MidpointRounding.AwayFromZero);
            return cursor;
        }

        public static double UncompressFixedPoint(uint cursor, long minRange, long maxRange, double precision)
        {
            var maxSerializedValue = GetMaxCursorForFixedPointCompression(minRange, maxRange, precision, out long range);

            return (((double)cursor / maxSerializedValue) * range) + minRange;
        }

        public static double CompressFixedPoint(double value, long minRange, long maxRange, double precision)
        {
            var cursor = CalculateCursorForFixedFloatCompression(value, minRange, maxRange, precision, out _);
            return UncompressFixedPoint(cursor, minRange, maxRange, precision);
        }

        public static (uint, uint, uint, uint) CalculateCursorsForQuaternionCompression(in Quaternion q, int bitsPerComponent)
        {
            if (bitsPerComponent < 2) throw new ArgumentOutOfRangeException(nameof(bitsPerComponent), bitsPerComponent, "Must have at least 2 bits");

            // Note: the reason for compression preserving the sign is we want a precise
            // representation of [-1, 0, 1], which would be not possible if we divided the
            // value range (<-1,1> = 2) evenly across whole available bits. Consider the 2
            // bit case - currently the max value is 1, but we can always represent -1, 0 and 1.
            // Now if we took the range approach and divided 2 by 3 (max value for 2 bits)
            // we could represent 1 more value, but we'd be stuck with -1, -0.33, 0.33, 1,
            // which produces some nasty artifacts on deserialization.
            double maxSerializedValue = Math.Pow(2, bitsPerComponent - 1) - 1;

            // On Mac casting directly from float to uint resulted in clamping, that is
            // negative values ended up as 0, whereas on Windows it simply reinterpreted
            // bits. Double cast prevents the issue maintaining the value bit-wise.
            uint xCursor = (uint)(int)Math.Round(q.X * maxSerializedValue);
            uint yCursor = (uint)(int)Math.Round(q.Y * maxSerializedValue);
            uint zCursor = (uint)(int)Math.Round(q.Z * maxSerializedValue);
            uint sign = q.W < 0 ? 1u : 0;

            return (xCursor, yCursor, zCursor, sign);
        }

        public static Quaternion UncompressQuaternion(int bitsPerComponent, int xCursor, int yCursor, int zCursor, uint wSign)
        {
            if (bitsPerComponent < 2) throw new ArgumentOutOfRangeException(nameof(bitsPerComponent), bitsPerComponent, "Must have at least 2 bits");

            double maxSerializedValue = Math.Pow(2, bitsPerComponent - 1) - 1d;

            int signBit = 1 << (bitsPerComponent - 1);
            int negativeMask = (~0) << bitsPerComponent;

            if ((xCursor & signBit) > 0)
            {
                xCursor |= negativeMask;
            }

            double x = xCursor / maxSerializedValue;

            if ((yCursor & signBit) > 0)
            {
                yCursor |= negativeMask;
            }

            double y = yCursor / maxSerializedValue;

            if ((zCursor & signBit) > 0)
            {
                zCursor |= negativeMask;
            }

            double z = zCursor / maxSerializedValue;

            double xyzSquareSum = x * x + y * y + z * z;
            if (xyzSquareSum < 1d)
            {
                double wSquared = 1d - xyzSquareSum;
                double w = Math.Sqrt(wSquared);
                if (wSign != 0)
                {
                    w = -w;
                }

                // Normalizing since compression might have affected magnitude
                double magnitudeFactor = 1d / Math.Sqrt(xyzSquareSum + wSquared);

                return new Quaternion((float)(x * magnitudeFactor), (float)(y * magnitudeFactor), (float)(z * magnitudeFactor), (float)(w * magnitudeFactor));
            }
            else
            {
                // A special case where due to the compression we've lost the precise value of W
                double magnitudeFactor = 1d / Math.Sqrt(xyzSquareSum);

                return new Quaternion((float)(x * magnitudeFactor), (float)(y * magnitudeFactor), (float)(z * magnitudeFactor), 0f);
            }
        }

        public static Quaternion CompressQuaternion(Quaternion quaternion, int bitsPerComponent)
        {
            var (xCursor, yCursor, zCursor, sign) = CalculateCursorsForQuaternionCompression(quaternion, bitsPerComponent);
            return UncompressQuaternion(bitsPerComponent, (int)xCursor, (int)yCursor, (int)zCursor, sign);
        }
    }
}
