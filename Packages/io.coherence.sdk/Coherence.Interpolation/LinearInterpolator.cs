// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using UnityEngine;
    using System;

    /// <summary>
    /// The LinearInterpolator performs a linear blend between two adjacent samples.
    /// The LinearInterpolator is defined for types int, long, float, Vector2, Vector3, Color and Quaternion (using <see cref="Quaternion.Slerp"/>).
    /// </summary>
    public class LinearInterpolator : Interpolator
    {
        static float FloatLerp(float a, float b, float t)
        {
            if (a == b)
            {
                return a;
            }

            var result = a * (1 - t) + b * t;
            if (float.IsPositiveInfinity(result))
            {
                result = float.MaxValue;
            }
            else if (float.IsNegativeInfinity(result))
            {
                result = float.MinValue;
            }

            return result;
        }

        static double DoubleLerp(double a, double b, float t)
        {
            if (a == b)
            {
                return a;
            }

            var result = a * (1 - t) + b * t;
            if (double.IsPositiveInfinity(result))
            {
                result = double.MaxValue;
            }
            else if (double.IsNegativeInfinity(result))
            {
                result = double.MinValue;
            }

            return result;
        }

        static long IntegerLerp(long a, long b, float t)
        {
            // casting t to double is necessary because int.MaxValue does not fit in float
            return a + (long)Math.Round((b - a) * (double)t);
        }

        public override int NumberOfSamplesToStayBehind => 1;

        public override byte InterpolateByte(byte value1, byte value2, float t)
        {
            return (byte)Math.Clamp(IntegerLerp(value1, value2, t), byte.MinValue, byte.MaxValue);
        }

        public override sbyte InterpolateSByte(sbyte value1, sbyte value2, float t)
        {
            return (sbyte)Math.Clamp(IntegerLerp(value1, value2, t), sbyte.MinValue, sbyte.MaxValue);
        }

        public override short InterpolateShort(short value1, short value2, float t)
        {
            return (short)Math.Clamp(IntegerLerp(value1, value2, t), short.MinValue, short.MaxValue);
        }

        public override ushort InterpolateUShort(ushort value1, ushort value2, float t)
        {
            return (ushort)Math.Clamp(IntegerLerp(value1, value2, t), ushort.MinValue, ushort.MaxValue);
        }

        public override int InterpolateInt(int value1, int value2, float t)
        {
            return (int)Math.Clamp(IntegerLerp(value1, value2, t), int.MinValue, int.MaxValue);
        }

        public override uint InterpolateUInt(uint value1, uint value2, float t)
        {
            return (uint)Math.Clamp(IntegerLerp(value1, value2, t), uint.MinValue, uint.MaxValue);
        }

        public override long InterpolateLong(long value1, long value2, float t)
        {
            // casting t to decimal is necessary because long.MaxValue does not fit in double
            var dt = (decimal)t;
            var decimalResult = Math.Round(value1 * (1 - dt) + value2 * dt);
            return (long)Math.Clamp(decimalResult, long.MinValue, long.MaxValue);
        }

        // ulong and long cannot use same implementation (as int and uint can) because ulong has bigger range in positive direction than long
        public override ulong InterpolateULong(ulong value1, ulong value2, float t)
        {
            // casting t to decimal is necessary because long.MaxValue does not fit in double
            var dt = (decimal)t;
            var decimalResult = Math.Round(value1 * (1 - dt) + value2 * dt);
            return (ulong)Math.Clamp(decimalResult, ulong.MinValue, ulong.MaxValue);
        }

        public override float InterpolateFloat(float value1, float value2, float t)
        {
            return FloatLerp(value1, value2, t);
        }

        public override double InterpolateDouble(double value1, double value2, float t)
        {
            return DoubleLerp(value1, value2, t);
        }

        public override Vector2 InterpolateVector2(Vector2 value1, Vector2 value2, float t)
        {
            return new Vector2(FloatLerp(value1.x, value2.x, t), FloatLerp(value1.y, value2.y, t));
        }

        public override Vector3 InterpolateVector3(Vector3 value1, Vector3 value2, float t)
        {
            return new Vector3(FloatLerp(value1.x, value2.x, t), FloatLerp(value1.y, value2.y, t), FloatLerp(value1.z, value2.z, t));
        }

        public override Quaternion InterpolateQuaternion(Quaternion value1, Quaternion value2, float t)
        {
            return Quaternion.SlerpUnclamped(value1, value2, t);
        }

        public override Color InterpolateColor(Color value1, Color value2, float t)
        {
            return new Color(FloatLerp(value1.r, value2.r, t), FloatLerp(value1.g, value2.g, t), FloatLerp(value1.b, value2.b, t), FloatLerp(value1.a, value2.a, t));
        }
    }
}
