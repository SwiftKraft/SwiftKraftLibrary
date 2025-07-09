// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    internal static class ArchetypeMath
    {
        private static Dictionary<Type, (double min, double max)> limitsByType =
            new Dictionary<Type, (double min, double max)>
            {
                [typeof(UInt32)] = (UInt32.MinValue, UInt32.MaxValue),
                [typeof(Int32)] = (Int32.MinValue, Int32.MaxValue),
                [typeof(Single)] = (Single.MinValue, Single.MaxValue),
                [typeof(Double)] = (Double.MinValue, Double.MaxValue),
            };

        private static Dictionary<Type, string> simpleTypeAliasByType =
            new Dictionary<Type, string>
            {
                [typeof(Byte)] = "byte",
                [typeof(SByte)] = "sbyte",
                [typeof(Char)] = "char",
                [typeof(UInt16)] = "ushort",
                [typeof(Int16)] = "short",
                [typeof(UInt32)] = "uint",
                [typeof(Int32)] = "int",
                [typeof(UInt64)] = "ulong",
                [typeof(Int64)] = "long",
                [typeof(Single)] = "float",
                [typeof(Double)] = "double",
            };

        internal static ulong GetTotalRangeByBitsAndPrecision(int bits, double precision)
        {
            return (ulong)((Math.Pow(2, bits) - 1) * precision);
        }

        internal static (long minRange, long maxRange) GetRangeByBitsAndPrecision(int bits, double precision)
        {
            ulong totalRange = GetTotalRangeByBitsAndPrecision(bits, precision);
            var minRange = -(long)(totalRange * 0.5d);
            var maxRange = (long)(totalRange * 0.5d);

            if ((ulong)(maxRange - minRange) < totalRange)
            {
                maxRange++;
            }

            return (minRange, maxRange);
        }

        internal static double GetPrecisionByBitsAndRange(int bits, ulong range)
        {
            return range / (Math.Pow(2, bits) - 1);
        }

        internal static double GetRoundedPrecisionByBitsAndRange(int bits, ulong range)
        {
            double precision = range / (Math.Pow(2, bits) - 1);
            uint precisionLog = (uint)Math.Log10((ulong)(1 / precision));
            return 1d / Math.Pow(10, precisionLog);
        }

        internal static double GetTruncatedFloatErrorPercentageByBits(int bits)
        {
            const int floatBaseBits = 9;

            if (bits <= floatBaseBits)
            {
                throw new ArgumentOutOfRangeException(nameof(bits), bits,
                    $"Invalid number of bits. Must be more than {floatBaseBits}");
            }

            int mantissaBits = bits - floatBaseBits;

            // The mantissa allow for 2^mantissaBits unique values within any exponent.
            // These values are spaced out equally between 2^exponent and 2^(exponent+1),
            // so each interval occupies a fraction of that distance. First value in the
            // interval (2^exponent) is also a step, so a total number of steps is actually
            // 1 + 2^mantissaBits.
            return 100d / (1d + Mathf.Pow(2, mantissaBits));
        }

        internal static int GetBitsMultiplier(SchemaType schemaType)
        {
            switch (schemaType)
            {
                case SchemaType.Vector2: return 2;
                case SchemaType.Vector3: return 3;
                case SchemaType.Quaternion: return 3;
                case SchemaType.Color: return 4;
            }
            return 1;
        }

        internal static int GetBitsForIntValue(long minRangeInclusive, long maxRangeInclusive)
        {
            double range = Math.Abs(maxRangeInclusive - minRangeInclusive) + 1; // Range is inclusive, e.g. <min,max> of <0,1> has range of 2

            if (range == 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(Math.Log(range, 2));
        }

        /// <summary>Clamps value between type's MinValue and MaxValue.</summary>
        internal static long ClampWithinTypeLimits(double value, Type valueType, out bool clamped)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported value");
            }

            if (!limitsByType.TryGetValue(valueType, out (double min, double max) limits))
            {
                throw new ArgumentOutOfRangeException(nameof(valueType), valueType, "Unsupported value type");
            }

            if (value < limits.min)
            {
                clamped = true;
                return (long)Math.Round(limits.min);
            }

            if (value > limits.max)
            {
                clamped = true;
                return (long)Math.Round(limits.max);
            }

            clamped = false;
            return (long)Math.Round(value);
        }

        internal static (double minRange, double maxRange) GetTypeLimits(Type valueType)
        {
            if (!limitsByType.TryGetValue(valueType, out (double min, double max) limits))
            {
                throw new ArgumentOutOfRangeException(nameof(valueType), valueType, "Unsupported value type");
            }

            return limits;
        }

        internal static bool TryGetTypeLimits(Type valueType, out double minRange, out double maxRange)
        {
            if (!limitsByType.TryGetValue(valueType, out (double min, double max) limits))
            {
                minRange = 0;
                maxRange = 0;

                return false;
            }

            minRange = limits.min;
            maxRange = limits.max;

            return true;
        }

        internal static string GetSimpleTypeAlias(Type valueType)
        {
            if (!simpleTypeAliasByType.TryGetValue(valueType, out string alias))
            {
                throw new ArgumentOutOfRangeException(nameof(valueType), valueType, "Unsupported value type");
            }

            return alias;
        }

        internal static bool TryGetBitsForFixedFloatValue(long minRange, long maxRange, double precision, out int bits)
        {
            try
            {
                bits = Brook.Utils.GetNumberOfBitsForFixedPointCompression(minRange, maxRange, precision, out _, out _);
                return true;
            }
            catch
            {
                bits = 0;
                return false;
            }
        }

        internal static bool CanOverride(SchemaType schemaType)
        {
            switch (schemaType)
            {
                case SchemaType.Int:
                case SchemaType.UInt:
                case SchemaType.Vector2:
                case SchemaType.Float:
                case SchemaType.Vector3:
                case SchemaType.Color:
                    return true;
            }
            return false;
        }
    }
}
