// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;
    using Coherence.Serializer;

    [Serializable]
    internal class BindingLODStepData
    {
        internal const int FLOAT_DEFAULT_BITS = 32;
        internal const double FLOAT_MIN_PRECISION = 0.1;
        internal const double FLOAT_DEFAULT_PRECISION = 0.001;

        internal const int FLOAT64_DEFAULT_BITS = 64;

        public SchemaType SchemaType { get => type; internal set => type = value; }
        internal bool IsFloatType => BindingArchetypeData.IsFloatBased(type);

        public int Bits => bits;
        public double Precision => precision;
        internal FloatCompression FloatCompression => floatCompression;

        [SerializeField] private SchemaType type;
        [SerializeField] private int bits;
        [SerializeField] private double precision;
        [SerializeField] private FloatCompression floatCompression;

        private int level;

        internal BindingLODStepData(BindingArchetypeData data, int level)
        {
            SetToData(data, level);
            SetDefaultOverrides(data.SchemaType);
            Verify(data.MinRange, data.MaxRange);
        }

        internal BindingLODStepData(BindingLODStepData other, BindingArchetypeData data)
        {
            CopyFrom(other, data);
        }

        internal void CopyFrom(BindingLODStepData other, BindingArchetypeData data)
        {
            type = other.type;
            precision = other.Precision;
            floatCompression = other.floatCompression;
            bits = other.Bits;

            SetToData(data, other.level);
        }

        private void SetToData(BindingArchetypeData data, int level)
        {
            this.level = level;
            type = data.SchemaType;
        }

        internal void SetDefaultOverrides(SchemaType type)
        {
            Overrides defaults = GetDefaultOverrides(type);

            precision = defaults.precision;
            bits = defaults.bits;
        }

        internal struct Overrides
        {
            public FloatCompression compression;
            public double precision;
            public int bits;
        }

        private static Overrides DefaultFloatOverride()
        {
            return new Overrides
            {
                compression = FloatCompression.None,
                bits = FLOAT_DEFAULT_BITS,
                precision = FLOAT_DEFAULT_PRECISION,
            };
        }

        private static Overrides DefaultFloat64Override()
        {
            return new Overrides
            {
                compression = FloatCompression.None,
                bits = FLOAT64_DEFAULT_BITS,
                precision = FLOAT_DEFAULT_PRECISION,
            };
        }

        internal static Overrides GetDefaultOverrides(SchemaType type)
        {
            switch (type)
            {
                case SchemaType.Int8:
                    return new Overrides() { bits = 8 };
                case SchemaType.UInt8:
                    return new Overrides() { bits = 8 };
                case SchemaType.Int16:
                    return new Overrides() { bits = 16 };
                case SchemaType.UInt16:
                    return new Overrides() { bits = 16 };
                case SchemaType.Char:
                    return new Overrides() { bits = 16 };
                case SchemaType.Int:
                    return new Overrides() { bits = 32 };
                case SchemaType.UInt:
                    return new Overrides() { bits = 32 };
                case SchemaType.Int64:
                    return new Overrides() { bits = 64 };
                case SchemaType.UInt64:
                    return new Overrides() { bits = 64 };
                case SchemaType.Float:
                case SchemaType.Vector2:
                case SchemaType.Vector3:
                    return DefaultFloatOverride();
                case SchemaType.Float64:
                    return DefaultFloat64Override();
                case SchemaType.String:
                    return new Overrides() { bits = OutProtocolBitStream.SHORT_STRING_LENGTH_BITS };
                case SchemaType.Quaternion:
                    return new Overrides() { bits = 32 };
                case SchemaType.Color:
                    return new Overrides() { bits = 32 };
                case SchemaType.Entity:
                    return new Overrides() { bits = 20 };
                case SchemaType.Bool:
                    return new Overrides() { bits = 1 };
                case SchemaType.Bytes:
                    return new Overrides() { bits = OutProtocolBitStream.MAX_BYTE_ARRAY_LENGTH * 8 };
                case SchemaType.Unknown:
                    return new Overrides();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported schema type");
            }
        }


        internal static int GetDefaultBits(SchemaType type)
        {
            return GetDefaultOverrides(type).bits * ArchetypeMath.GetBitsMultiplier(type);
        }

        internal bool SetPrecision(double precision)
        {
            if (precision != this.precision)
            {
                this.precision = precision;
                return true;
            }

            return false;
        }

        internal void SetFloatCompression(FloatCompression floatCompression)
        {
            this.floatCompression = floatCompression;
        }

        internal void SetBits(int bits)
        {
            this.bits = bits;
        }

        internal void Verify(long minRange, long maxRange)
        {
            switch (SchemaType)
            {
                case SchemaType.Color:
                    VerifyColor();
                    return;
                case SchemaType.Quaternion:
                    VerifyQuaternion();
                    return;
                case SchemaType.Int:
                case SchemaType.UInt:
                    bits = ArchetypeMath.GetBitsForIntValue(minRange, maxRange);
                    return;
            }

            if (IsFloatType)
            {
                VerifyFloat(minRange, maxRange);
                return;
            }
        }

        private void VerifyQuaternion()
        {
            int sanitizedBits = Mathf.Clamp(bits, 2, 32);
            SetBits(sanitizedBits);

            // Note: this is a crude approximation. In reality it's non-trivial
            // to calculate the precision since the angle is calculated using
            // multiplication, addition and trigonometric functions on the components.
            double anglePrecision = 360d / Math.Pow(2, sanitizedBits);
            SetPrecision(anglePrecision);
        }

        private void VerifyColor()
        {
            int sanitizedBits = Mathf.Clamp(bits, 1, 32);
            SetBits(sanitizedBits);

            double precision = ArchetypeMath.GetPrecisionByBitsAndRange(sanitizedBits, 1);
            SetPrecision(precision);
        }

        private void VerifyFloat(long minRange, long maxRange)
        {
            ulong range = (ulong)(maxRange - minRange);

            switch (floatCompression)
            {
                case FloatCompression.None:
                    SetBits(sizeof(float) * 8);
                    return;

                case FloatCompression.Truncated:
                    {
                        int sanitizedBits = Mathf.Clamp(bits, 10, 32);
                        if (sanitizedBits != bits)
                        {
                            SetBits(sanitizedBits);
                        }
                        SetPrecision(ArchetypeMath.GetTruncatedFloatErrorPercentageByBits(sanitizedBits));
                    }
                    break;

                case FloatCompression.FixedPoint:
                    {
                        // Favour precision. If we can't represent precision then adjust it.
                        if (ArchetypeMath.TryGetBitsForFixedFloatValue(minRange, maxRange, precision, out int newBits))
                        {
                            SetBits(newBits);
                        }

                        double sanitizedPrecision = Math.Min(ArchetypeMath.GetRoundedPrecisionByBitsAndRange(bits, range), FLOAT_MIN_PRECISION);
                        if (sanitizedPrecision > precision)
                        {
                            SetPrecision(sanitizedPrecision);
                        }
                    }
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(floatCompression), floatCompression, $"Unsupported {nameof(Coherence.FloatCompression)}");
            }
        }

        public int TotalBits
        {
            get
            {
                int totalBits = Bits * ArchetypeMath.GetBitsMultiplier(SchemaType);
                if (SchemaType == SchemaType.Quaternion)
                {
                    // Quaternion uses additional sign bit
                    totalBits += 1;
                }

                return totalBits;
            }
        }

        internal void UpdateModel(BindingArchetypeData model, int level, bool forceUpdate = false)
        {
            bool shouldUpdateDefaultValue = type != model.SchemaType;
            if (shouldUpdateDefaultValue || forceUpdate)
            {
                SetDefaultOverrides(model.SchemaType);
            }

            SetToData(model, level);
        }

        internal bool IsOverriding
        {
            get
            {
                var defaults = GetDefaultOverrides(type);

                switch (type)
                {
                    case SchemaType.Float:
                    case SchemaType.Vector2:
                    case SchemaType.Vector3:
                        return Bits != defaults.bits || floatCompression != defaults.compression || precision != defaults.precision;
                    case SchemaType.Quaternion:
                    case SchemaType.Color:
                        return Bits != defaults.bits;
                    default:
                        return false;
                }
            }
        }
    }
}
