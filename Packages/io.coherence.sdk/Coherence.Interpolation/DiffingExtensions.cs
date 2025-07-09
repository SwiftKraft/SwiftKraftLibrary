// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Methods duplicated from <see cref="Coherence.Toolkit.CoherenceToUnityConverters"/> to prevent cyclic package dependencies.
    /// </summary>
    internal static class DiffingExtensions
    {
        public static double EpsilonDouble = 0.001d;
        public static float EpsilonFloat = 0.001f;

        public static bool DiffersFrom(this double a, double b, double epsilonDouble)
        {
            return Math.Abs(a - b) > epsilonDouble;
        }

        public static bool DiffersFrom(this float a, float b, float epsilonFloat)
        {
            return Mathf.Abs(a - b) > epsilonFloat;
        }

        public static bool DiffersFrom(this Vector2 a, Vector2 b, float epsilonFloat)
        {
            if (DiffersFrom(a.x, b.x, epsilonFloat)) return true;
            if (DiffersFrom(a.y, b.y, epsilonFloat)) return true;

            return false;
        }

        public static bool DiffersFrom(this Vector2 a, Vector2 b)
        {
            return a.DiffersFrom(b, EpsilonFloat);
        }

        public static bool DiffersFrom(this Vector3 a, Vector3 b, float epsilonFloat)
        {
            if (DiffersFrom(a.x, b.x, epsilonFloat)) return true;
            if (DiffersFrom(a.y, b.y, epsilonFloat)) return true;
            if (DiffersFrom(a.z, b.z, epsilonFloat)) return true;

            return false;
        }

        public static bool DiffersFrom(this Vector3 a, Vector3 b)
        {
            return a.DiffersFrom(b, EpsilonFloat);
        }

        public static bool DiffersFrom(this Quaternion a, Quaternion b, float epsilonFloat)
        {
            if (DiffersFrom(a.x, b.x, epsilonFloat)) return true;
            if (DiffersFrom(a.y, b.y, epsilonFloat)) return true;
            if (DiffersFrom(a.z, b.z, epsilonFloat)) return true;
            if (DiffersFrom(a.w, b.w, epsilonFloat)) return true;

            return false;
        }

        public static bool DiffersFrom(this Quaternion a, Quaternion b)
        {
            return a.DiffersFrom(b, EpsilonFloat);
        }
    }
}
