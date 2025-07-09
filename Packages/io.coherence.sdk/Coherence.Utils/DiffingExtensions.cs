// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils
{
    using System;
    using System.Numerics;

    public static class DiffingExtensions
    {
        internal const float EpsilonFloat = 0.001f;
        internal const double EpsilonDouble = 0.001d;

        public static bool DiffersFrom(this string a, string b)
        {
            return a != b;
        }

        public static bool DiffersFrom(this byte[] a, byte[] b)
        {
            if (a == b)
            {
                return false;
            }

            if (a == null || b == null)
            {
                return true;
            }

            if (a.Length != b.Length)
            {
                return true;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return true;
                }
            }

            return false;
        }

        public static bool DiffersFrom(this double a, double b)
        {
            return Math.Abs(a - b) > EpsilonDouble;
        }

        public static bool DiffersFrom(this float a, float b)
        {
            return Math.Abs(a - b) > EpsilonFloat;
        }

        public static bool DiffersFrom(this Vector2 a, Vector2 b)
        {
            if (DiffersFrom(a.X, b.X)) return true;
            if (DiffersFrom(a.Y, b.Y)) return true;

            return false;
        }

        public static bool DiffersFrom(this Vector3 a, Vector3 b)
        {
            if (DiffersFrom(a.X, b.X)) return true;
            if (DiffersFrom(a.Y, b.Y)) return true;
            if (DiffersFrom(a.Z, b.Z)) return true;

            return false;
        }

        public static bool DiffersFrom(this Vector4 a, Vector4 b)
        {
            if (DiffersFrom(a.X, b.X)) return true;
            if (DiffersFrom(a.Y, b.Y)) return true;
            if (DiffersFrom(a.Z, b.Z)) return true;
            if (DiffersFrom(a.W, b.W)) return true;

            return false;
        }

        public static bool DiffersFrom(this Quaternion a, Quaternion b)
        {
            if (DiffersFrom(a.X, b.X)) return true;
            if (DiffersFrom(a.Y, b.Y)) return true;
            if (DiffersFrom(a.Z, b.Z)) return true;
            if (DiffersFrom(a.W, b.W)) return true;

            return false;
        }
    }
}
