// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using System;
using Coherence.Entities;
using UnityEngine;

namespace Coherence.Interpolation
{
    /// <summary>
    /// IInterpolator interface defines methods used for interpolating between given samples.
    /// </summary>
    /// <typeparam name="T">Type of the interpolated samples</typeparam>
    internal interface IInterpolator<T>
    {
        /// <summary>
        /// Interpolates and returns a new value between the given values based on the given t.
        /// </summary>
        /// <param name="value0">Value of the sample which is before the first interpolation sample</param>
        /// <param name="value1">Value of the first interpolation sample</param>
        /// <param name="value2">Value of the second interpolation sample</param>
        /// <param name="value3">Value of the sample which is after the second interpolation sample</param>
        /// <param name="t">Value in range [0, 1] indicating interpolation percentage between first and second sample</param>
        /// <returns>Interpolated value</returns>
        T Interpolate(T value0, T value1, T value2, T value3, float t);
    }

    /// <summary>
    /// Base class used for blending between sample points.
    /// The base implementation simply provides latest data sampling so that values "snap" to the sample that is latest.
    /// By sub-classing and overriding these methods, more refined interpolation can be achieved (see <see cref="LinearInterpolator"/> and <see cref="SplineInterpolator"/>).
    /// Interpolation can be implemented either using 2 samples or 4 samples.
    /// Methods are specified for all supported types, taking sample values as parameters together with the time t that is expressed as a fraction between value1 and value2.
    /// Unless the 4 sample methods are overriden, they will simply call the corresponding 2 sample methods that will suffice for most types of interpolation.
    /// </summary>
    [Serializable]
    public class Interpolator :
        IInterpolator<float>, IInterpolator<double>, IInterpolator<bool>, IInterpolator<byte>, IInterpolator<sbyte>,
        IInterpolator<short>, IInterpolator<ushort>, IInterpolator<char>, IInterpolator<int>, IInterpolator<uint>,
        IInterpolator<long>, IInterpolator<ulong>, IInterpolator<Vector2>, IInterpolator<Vector3>, IInterpolator<Quaternion>,
        IInterpolator<Color>, IInterpolator<string>, IInterpolator<byte[]>, IInterpolator<Entity>
    {
        /// <summary>
        /// Number of leading samples required for this interpolation method, added to <see cref="InterpolationSettings.Delay"/>.
        /// Linear interpolation requires one sample ahead (and one behind) to produce valid results while splines require two samples ahead and behind.
        /// The default Interpolator requires no samples ahead or behind because since it simply applies the most recent sample with no blending.
        /// </summary>
        public virtual int NumberOfSamplesToStayBehind => 0;

        // 4 sample implementation
        public virtual float InterpolateFloat(float value0, float value1, float value2, float value3, float t) { return InterpolateFloat(value1, value2, t); }
        public virtual double InterpolateDouble(double value0, double value1, double value2, double value3, float t) { return InterpolateDouble(value1, value2, t); }
        public virtual bool InterpolateBoolean(bool value0, bool value1, bool value2, bool value3, float t) { return InterpolateBoolean(value1, value2, t); }
        public virtual byte InterpolateByte(byte value0, byte value1, byte value2, byte value3, float t) { return InterpolateByte(value1, value2, t); }
        public virtual sbyte InterpolateSByte(sbyte value0, sbyte value1, sbyte value2, sbyte value3, float t) { return InterpolateSByte(value1, value2, t); }
        public virtual short InterpolateShort(short value0, short value1, short value2, short value3, float t) { return InterpolateShort(value1, value2, t); }
        public virtual ushort InterpolateUShort(ushort value0, ushort value1, ushort value2, ushort value3, float t) { return InterpolateUShort(value1, value2, t); }
        public virtual char InterpolateChar(char value0, char value1, char value2, char value3, float t) { return InterpolateChar(value1, value2, t); }
        public virtual int InterpolateInt(int value0, int value1, int value2, int value3, float t) { return InterpolateInt(value1, value2, t); }
        public virtual uint InterpolateUInt(uint value0, uint value1, uint value2, uint value3, float t) { return InterpolateUInt(value1, value2, t); }
        public virtual long InterpolateLong(long value0, long value1, long value2, long value3, float t) { return InterpolateLong(value1, value2, t); }
        public virtual ulong InterpolateULong(ulong value0, ulong value1, ulong value2, ulong value3, float t) { return InterpolateULong(value1, value2, t); }
        public virtual Vector2 InterpolateVector2(Vector2 value0, Vector2 value1, Vector2 value2, Vector2 value3, float t) { return InterpolateVector2(value1, value2, t); }
        public virtual Vector3 InterpolateVector3(Vector3 value0, Vector3 value1, Vector3 value2, Vector3 value3, float t) { return InterpolateVector3(value1, value2, t); }
        public virtual Quaternion InterpolateQuaternion(Quaternion value0, Quaternion value1, Quaternion value2, Quaternion value3, float t) { return InterpolateQuaternion(value1, value2, t); }
        public virtual Color InterpolateColor(Color value0, Color value1, Color value2, Color value3, float t) { return InterpolateColor(value1, value2, t); }
        public virtual string InterpolateString(string value0, string value1, string value2, string value3, float t) { return InterpolateString(value1, value2, t); }
        public virtual byte[] InterpolateBytes(byte[] value0, byte[] value1, byte[] value2, byte[] value3, float t) { return InterpolateBytes(value1, value2, t); }
        public virtual Entity InterpolateEntityReference(Entity value0, Entity value1, Entity value2, Entity value3, float t) { return InterpolateEntityReference(value1, value2, t); }

        // 2 sample implementation
        public virtual float InterpolateFloat(float value1, float value2, float t) { return value2; }
        public virtual double InterpolateDouble(double value1, double value2, float t) { return value2; }
        public virtual bool InterpolateBoolean(bool value1, bool value2, float t) { return value2; }
        public virtual byte InterpolateByte(byte value1, byte value2, float t) { return value2; }
        public virtual sbyte InterpolateSByte(sbyte value1, sbyte value2, float t) { return value2; }
        public virtual short InterpolateShort(short value1, short value2, float t) { return value2; }
        public virtual ushort InterpolateUShort(ushort value1, ushort value2, float t) { return value2; }
        public virtual char InterpolateChar(char value1, char value2, float t) { return value2; }
        public virtual int InterpolateInt(int value1, int value2, float t) { return value2; }
        public virtual uint InterpolateUInt(uint value1, uint value2, float t) { return value2; }
        public virtual long InterpolateLong(long value1, long value2, float t) { return value2; }
        public virtual ulong InterpolateULong(ulong value1, ulong value2, float t) { return value2; }
        public virtual Vector2 InterpolateVector2(Vector2 value1, Vector2 value2, float t) { return value2; }
        public virtual Vector3 InterpolateVector3(Vector3 value1, Vector3 value2, float t) { return value2; }
        public virtual Quaternion InterpolateQuaternion(Quaternion value1, Quaternion value2, float t) { return value2; }
        public virtual Color InterpolateColor(Color value1, Color value2, float t) { return value2; }
        public virtual string InterpolateString(string value1, string value2, float t) { return value2; }
        public virtual byte[] InterpolateBytes(byte[] value1, byte[] value2, float t) { return value2; }
        public virtual Entity InterpolateEntityReference(Entity value1, Entity value2, float t) { return value2; }

        // IInterpolator<T> implementations
        public float Interpolate(float value0, float value1, float value2, float value3, float t) => InterpolateFloat(value0, value1, value2, value3, t);
        public double Interpolate(double value0, double value1, double value2, double value3, float t) => InterpolateDouble(value0, value1, value2, value3, t);
        public bool Interpolate(bool value0, bool value1, bool value2, bool value3, float t) => InterpolateBoolean(value0, value1, value2, value3, t);
        public byte Interpolate(byte value0, byte value1, byte value2, byte value3, float t) => InterpolateByte(value0, value1, value2, value3, t);
        public sbyte Interpolate(sbyte value0, sbyte value1, sbyte value2, sbyte value3, float t) => InterpolateSByte(value0, value1, value2, value3, t);
        public short Interpolate(short value0, short value1, short value2, short value3, float t) => InterpolateShort(value0, value1, value2, value3, t);
        public ushort Interpolate(ushort value0, ushort value1, ushort value2, ushort value3, float t) => InterpolateUShort(value0, value1, value2, value3, t);
        public char Interpolate(char value0, char value1, char value2, char value3, float t) => InterpolateChar(value0, value1, value2, value3, t);
        public int Interpolate(int value0, int value1, int value2, int value3, float t) => InterpolateInt(value0, value1, value2, value3, t);
        public uint Interpolate(uint value0, uint value1, uint value2, uint value3, float t) => InterpolateUInt(value0, value1, value2, value3, t);
        public long Interpolate(long value0, long value1, long value2, long value3, float t) => InterpolateLong(value0, value1, value2, value3, t);
        public ulong Interpolate(ulong value0, ulong value1, ulong value2, ulong value3, float t) => InterpolateULong(value0, value1, value2, value3, t);
        public Vector2 Interpolate(Vector2 value0, Vector2 value1, Vector2 value2, Vector2 value3, float t) => InterpolateVector2(value0, value1, value2, value3, t);
        public Vector3 Interpolate(Vector3 value0, Vector3 value1, Vector3 value2, Vector3 value3, float t) => InterpolateVector3(value0, value1, value2, value3, t);
        public Quaternion Interpolate(Quaternion value0, Quaternion value1, Quaternion value2, Quaternion value3, float t) => InterpolateQuaternion(value0, value1, value2, value3, t);
        public Color Interpolate(Color value0, Color value1, Color value2, Color value3, float t) => InterpolateColor(value0, value1, value2, value3, t);
        public string Interpolate(string value0, string value1, string value2, string value3, float t) => InterpolateString(value0, value1, value2, value3, t);
        public byte[] Interpolate(byte[] value0, byte[] value1, byte[] value2, byte[] value3, float t) => InterpolateBytes(value0, value1, value2, value3, t);
        public Entity Interpolate(Entity value0, Entity value1, Entity value2, Entity value3, float t) => InterpolateEntityReference(value0, value1, value2, value3, t);
    }
}
