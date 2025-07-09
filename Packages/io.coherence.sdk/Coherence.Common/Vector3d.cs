namespace Coherence.Common
{
    using System;
    using System.Numerics;

    public struct Vector3d
    {
        public double x;
        public double y;
        public double z;

        public Vector3d normalized => Vector3d.Normalize(this);
        public double magnitude => Math.Sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
        public double sqrMagnitude => this.x * this.x + this.y * this.y + this.z * this.z;

        public static Vector3d zero => new Vector3d(0d, 0d, 0d);
        public static Vector3d one => new Vector3d(1d, 1d, 1d);
        public static Vector3d forward => new Vector3d(0d, 0d, 1d);
        public static Vector3d back => new Vector3d(0d, 0d, -1d);
        public static Vector3d up => new Vector3d(0d, 1d, 0d);
        public static Vector3d down => new Vector3d(0d, -1d, 0d);
        public static Vector3d left => new Vector3d(-1d, 0d, 0d);
        public static Vector3d right => new Vector3d(1d, 0d, 0d);
        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3d(float x, float y, float z)
        {
            this.x = (double)x;
            this.y = (double)y;
            this.z = (double)z;
        }

        public Vector3d(double x, double y)
        {
            this.x = x;
            this.y = y;
            this.z = 0d;
        }

        public static Vector3d operator +(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3d operator -(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3d operator -(Vector3d a)
        {
            return new Vector3d(-a.x, -a.y, -a.z);
        }

        public static Vector3d operator *(Vector3d a, double d)
        {
            return new Vector3d(a.x * d, a.y * d, a.z * d);
        }

        public static Vector3d operator *(double d, Vector3d a)
        {
            return new Vector3d(a.x * d, a.y * d, a.z * d);
        }

        public static Vector3d operator /(Vector3d a, double d)
        {
            return new Vector3d(a.x / d, a.y / d, a.z / d);
        }

        public static bool operator ==(Vector3d lhs, Vector3d rhs)
        {
            return SqrMagnitude(lhs - rhs) < 0.0 / 1.0;
        }

        public static bool operator !=(Vector3d lhs, Vector3d rhs)
        {
            return SqrMagnitude(lhs - rhs) >= 0.0 / 1.0;
        }

        public override int GetHashCode()
        {
            return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector3d))
                return false;
            Vector3d vector3d = (Vector3d)other;
            if (this.x.Equals(vector3d.x) && this.y.Equals(vector3d.y))
                return this.z.Equals(vector3d.z);
            else
                return false;
        }

        public static Vector3d Normalize(Vector3d value)
        {
            double num = Magnitude(value);
            if (num > 9.99999974737875E-06)
                return value / num;
            else
                return zero;
        }

        public void Normalize()
        {
            double num = Magnitude(this);
            if (num > 9.99999974737875E-06)
                this /= num;
            else
                this = zero;
        }

        public override string ToString()
        {
            return $"({this.x:F2}, {this.y:F2}, {this.z:F2})";
        }

        public static double Magnitude(Vector3d a)
        {
            return Math.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
        }

        public static double SqrMagnitude(Vector3d a)
        {
            return a.x * a.x + a.y * a.y + a.z * a.z;
        }

        public bool IsWithinRange(double maxRange)
        {
            return x <= maxRange && x >= -maxRange &&
                   y <= maxRange && y >= -maxRange &&
                   z <= maxRange && z >= -maxRange;
        }

        public Vector3 ToCoreVector3()
        {
            if (x < float.MinValue || x > float.MaxValue)
            {
                throw new OverflowException($"Cannot convert vector3d to vector3, component X is overflowing 32-bits ({x})");
            }

            if (y < float.MinValue || y > float.MaxValue)
            {
                throw new OverflowException($"Cannot convert vector3d to vector3, component Y is overflowing 32-bits ({y})");
            }

            if (z < float.MinValue || z > float.MaxValue)
            {
                throw new OverflowException($"Cannot convert vector3d to vector3, component Z is overflowing 32-bits ({z})");
            }

            return new Vector3((float)x, (float)y, (float)z);
        }
    }
}
