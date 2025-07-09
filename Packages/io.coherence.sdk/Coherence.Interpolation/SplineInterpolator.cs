// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using System;
    using UnityEngine;

    /// <summary>
    /// The SplineInterpolator blends sample values smoothly using a Catmull-Rom curve.
    /// The SplineInterpolator is defined for types float, Vector2 and Vector3.
    /// </summary>
    public class SplineInterpolator : Interpolator
    {
        // Adapted from https://qroph.github.io/2018/07/30/smooth-paths-using-catmull-rom-splines.html

        // Settings
        [SerializeField] private CurveType curveType = CurveType.Centripetal;
        [SerializeField] [Range(0, 1)] private float tension = 0;

        private enum CurveType
        {
            Uniform,
            Centripetal,
            Chordal,
        }

        private float Alpha =>
            curveType switch
            {
                CurveType.Uniform => 0,
                CurveType.Centripetal => 0.5f,
                CurveType.Chordal => 1f,
                _ => throw new ArgumentOutOfRangeException()
            };

        public override int NumberOfSamplesToStayBehind => 2;

        public override float InterpolateFloat(float p0, float p1, float p2, float p3, float t)
        {
            if (p1 - p2 == 0)
            {
                return p2;
            }

            // Extrapolate behind
            if (p0 - p1 == 0)
            {
                p0 = p1 + p1 - p2;
            }

            // Extrapolate ahead
            if (p2 - p3 == 0)
            {
                p3 = p2 + p2 - p1;
            }

            // Calculate knot sequence
            var t01 = Mathf.Pow((p0 - p1), Alpha);
            var t12 = Mathf.Pow((p1 - p2), Alpha);
            var t23 = Mathf.Pow((p2 - p3), Alpha);

            var m1 = (1.0f - tension) * (p2 - p1 + (t12 * (((p1 - p0) / t01) - ((p2 - p0) / (t01 + t12)))));
            var m2 = (1.0f - tension) * (p2 - p1 + (t12 * (((p3 - p2) / t23) - ((p3 - p1) / (t12 + t23)))));

            var a = (2.0f * (p1 - p2)) + m1 + m2;
            var b = (-3.0f * (p1 - p2)) - m1 - m1 - m2;
            var c = m1;
            var d = p1;

            var point = (a * t * t * t) +
                        (b * t * t) +
                        (c * t) +
                        d;

            // Return p2 if CMR fails (i.e. two samples have the same position)
            if (float.IsNaN(point))
            {
                return p2;
            }

            return point;
        }

        public override double InterpolateDouble(double p0, double p1, double p2, double p3, float t)
        {
            if (p1 - p2 == 0)
            {
                return p2;
            }

            // Extrapolate behind
            if (p0 - p1 == 0)
            {
                p0 = p1 + p1 - p2;
            }

            // Extrapolate ahead
            if (p2 - p3 == 0)
            {
                p3 = p2 + p2 - p1;
            }

            // Calculate knot sequence
            var t01 = Math.Pow((p0 - p1), Alpha);
            var t12 = Math.Pow((p1 - p2), Alpha);
            var t23 = Math.Pow((p2 - p3), Alpha);

            var m1 = (1.0d - tension) * (p2 - p1 + (t12 * (((p1 - p0) / t01) - ((p2 - p0) / (t01 + t12)))));
            var m2 = (1.0d - tension) * (p2 - p1 + (t12 * (((p3 - p2) / t23) - ((p3 - p1) / (t12 + t23)))));

            var a = (2.0d * (p1 - p2)) + m1 + m2;
            var b = (-3.0d * (p1 - p2)) - m1 - m1 - m2;
            var c = m1;
            var d = p1;

            var point = (a * t * t * t) +
                        (b * t * t) +
                        (c * t) +
                        d;

            // Return p2 if CMR fails (i.e. two samples have the same position)
            if (double.IsNaN(point))
            {
                return p2;
            }

            return point;
        }

        public override Vector2 InterpolateVector2(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            if (p1 == p2)
            {
                return p2;
            }

            // Extrapolate behind
            if (p0 == p1)
            {
                p0 = p1 + p1 - p2;
            }

            // Extrapolate ahead
            if (p2 == p3)
            {
                p3 = p2 + p2 - p1;
            }

            // Calculate knot sequence
            var t01 = Mathf.Pow((p0 - p1).magnitude, Alpha);
            var t12 = Mathf.Pow((p1 - p2).magnitude, Alpha);
            var t23 = Mathf.Pow((p2 - p3).magnitude, Alpha);

            var m1 = (1.0f - tension) * (p2 - p1 + (t12 * (((p1 - p0) / t01) - ((p2 - p0) / (t01 + t12)))));
            var m2 = (1.0f - tension) * (p2 - p1 + (t12 * (((p3 - p2) / t23) - ((p3 - p1) / (t12 + t23)))));

            var a = (2.0f * (p1 - p2)) + m1 + m2;
            var b = (-3.0f * (p1 - p2)) - m1 - m1 - m2;
            var c = m1;
            var d = p1;

            var point = (a * t * t * t) +
                        (b * t * t) +
                        (c * t) +
                        d;

            // Return p2 if CMR fails (i.e. two samples have the same position)
            if (float.IsNaN(point.x) || float.IsNaN(point.y))
            {
                return p2;
            }

            return point;
        }

        public override Vector3 InterpolateVector3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            if (p1 == p2)
            {
                return p2;
            }

            // Extrapolate behind
            if (p0 == p1)
            {
                p0 = p1 + p1 - p2;
            }

            // Extrapolate ahead
            if (p2 == p3)
            {
                p3 = p2 + p2 - p1;
            }

            // Calculate knot sequence
            var t01 = Mathf.Pow((p0 - p1).magnitude, Alpha);
            var t12 = Mathf.Pow((p1 - p2).magnitude, Alpha);
            var t23 = Mathf.Pow((p2 - p3).magnitude, Alpha);

            var m1 = (1.0f - tension) * (p2 - p1 + (t12 * (((p1 - p0) / t01) - ((p2 - p0) / (t01 + t12)))));
            var m2 = (1.0f - tension) * (p2 - p1 + (t12 * (((p3 - p2) / t23) - ((p3 - p1) / (t12 + t23)))));

            var a = (2.0f * (p1 - p2)) + m1 + m2;
            var b = (-3.0f * (p1 - p2)) - m1 - m1 - m2;
            var c = m1;
            var d = p1;

            var point = (a * t * t * t) +
                        (b * t * t) +
                        (c * t) +
                        d;

            // Return p2 if CMR fails (i.e. two samples have the same position)
            if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z))
            {
                return p2;
            }

            return point;
        }

    }
}
