// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils
{
    using Log;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public static class Bounds
    {
#if COHERENCE_DISABLE_POSITION_SANITIZATION
        private const string False = "HEQKNHYTDWJWJXPITVDR";
        [Conditional(False)]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPositionForNanAndInfinity(ref Vector3 value, Logger logger)
        {
            bool hasNanOrInfinity = false;
            Vector3 originalValue = value;

            SanitizeNanAndInfinity(ref value.X, ref hasNanOrInfinity);
            SanitizeNanAndInfinity(ref value.Y, ref hasNanOrInfinity);
            SanitizeNanAndInfinity(ref value.Z, ref hasNanOrInfinity);

            if (hasNanOrInfinity)
            {
                logger.Warning(Warning.BoundsPositionInvalid, ("position", originalValue));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            int t = value < min ? min : value;
            return t > max ? max : t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Clamp(uint value, uint min, uint max)
        {
            uint t = value < min ? min : value;
            return t > max ? max : t;
        }

        [Conditional("DEBUG")]
        public static void Check(float value, float min, float max, string variableName, Logger logger)
        {
            // Seems more fair to consider that the float value might not be
            // a whole number when checking the bounds.
            const float epsilon = 10e-4f;

            if (value < (min - epsilon) || value > (max + epsilon))
            {
                logger.Warning(Warning.BoundsVariable,
                    ("name", variableName),
                    ("allowed", $"[{min}, {max}]"),
                    ("actual", $"{value}"));
            }
        }

        [Conditional("DEBUG")]
        public static void Check(int value, int min, int max, string variableName, Logger logger)
        {
            if (value < min || value > max)
            {
                logger.Warning(Warning.BoundsVariable,
                    ("name", variableName),
                    ("allowed", $"[{min}, {max}]"),
                    ("actual", $"{value}"));
            }

        }

        [Conditional("DEBUG")]
        public static void Check(uint value, uint min, uint max, string variableName, Logger logger)
        {
            if (value < min || value > max)
            {
                logger.Warning(Warning.BoundsVariable,
                    ("name", variableName),
                    ("allowed", $"[{min}, {max}]"),
                    ("actual", $"{value}"));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SanitizeNanAndInfinity(ref float value, ref bool hasNanOrInfinity)
        {
            if (float.IsNaN(value))
            {
                value = 0f;
                hasNanOrInfinity = true;
            }

            if (float.IsPositiveInfinity(value))
            {
                value = float.MaxValue;
                hasNanOrInfinity = true;
            }

            if (float.IsNegativeInfinity(value))
            {
                value = float.MinValue;
                hasNanOrInfinity = true;
            }
        }
    }
}
