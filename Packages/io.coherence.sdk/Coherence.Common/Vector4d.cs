namespace Coherence.Common
{
    using System;
    using System.Numerics;

    public struct Vector4d
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public static Vector4d zero => new Vector4d(0, 0, 0, 0);

        public Vector4d normalized => Normalize(this);

        public Vector4d(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static Vector4d Normalize(Vector4d v)
        {
            var mag = Magnitude(v);
            if (mag > 1E-05f)
            {
                return v / mag;
            }

            return zero;
        }

        public static double Magnitude(Vector4d a)
        {
            return Math.Sqrt(Dot(a, a));
        }

        public static double Dot(Vector4d a, Vector4d b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        }

        public static Vector4d Project(Vector4d a, Vector4d b)
        {
            return b * (Dot(a, b) / Dot(b, b));
        }

        public static Vector4d operator *(Vector4d a, double d)
        {
            return new Vector4d(a.x * d, a.y * d, a.z * d, a.w * d);
        }

        public static Vector4d operator /(Vector4d a, double d)
        {
            return new Vector4d(a.x / d, a.y / d, a.z / d, a.w / d);
        }
    }
}
