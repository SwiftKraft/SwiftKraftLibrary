namespace Coherence.Interpolation.Tests
{
    using System;
    using UnityEngine;

    public static class Approx
    {
        public static readonly float FloatEpsilon = 1E-6f;

        public static readonly double DoubleEpsilon = 1E-6f;

        public static bool Eq(this Vector3 a, Vector3 b)
        {
            return a.x.Eq(b.x) && a.y.Eq(b.y) && a.z.Eq(b.z);
        }

        public static bool Eq(this Vector2 a, Vector2 b)
        {
            return a.x.Eq(b.x) && a.y.Eq(b.y);
        }

        public static bool Eq(this float a, float b)
        {
            if (float.IsNaN(a))
            {
                return float.IsNaN(b);
            }

            if (float.IsPositiveInfinity(a))
            {
                return float.IsPositiveInfinity(b);
            }

            if (float.IsNegativeInfinity(a))
            {
                return float.IsNegativeInfinity(b);
            }
            return Math.Abs(b - a) <= Math.Max(1E-06 * Math.Max(Math.Abs(a), Math.Abs(b)), FloatEpsilon * 8f);
        }

        public static bool Eq(this double a, double b)
        {
            if (double.IsNaN(a))
            {
                return double.IsNaN(b);
            }

            if (double.IsPositiveInfinity(a))
            {
                return double.IsPositiveInfinity(b);
            }

            if (double.IsNegativeInfinity(a))
            {
                return double.IsNegativeInfinity(b);
            }

            return Math.Abs(b - a) < Math.Max(1E-06 * Math.Max(Math.Abs(a), Math.Abs(b)), DoubleEpsilon * 8f);
        }
    }
}
