// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Coherence.Interpolation;
    using System.Linq;

    [Serializable]
    internal class BindingArchetypeData
    {
        internal bool IsFloatType => IsFloatBased(SchemaType);

        public SchemaType SchemaType { get => type; internal set => type = value; }
        public List<BindingLODStepData> Fields => fields;
        public long MinRange => minRange;
        public long MaxRange => maxRange;
        public ulong TotalRange => (ulong)Math.Round(maxRange - (double)minRange);
        public float SampleRate => sampleRate;
        internal FloatCompression FloatCompression => floatCompression;

        public readonly bool IsMethod;

        [SerializeField] protected SchemaType type;
        [SerializeField] protected long minRange = 0;
        [SerializeField] protected long maxRange = 0;
        [SerializeField] protected float sampleRate = InterpolationSettings.DefaultSampleRate;
        [SerializeField] internal FloatCompression floatCompression;

        [SerializeField] protected List<BindingLODStepData> fields = new List<BindingLODStepData>();

        public BindingArchetypeData(SchemaType type, Type valueType, bool isMethod)
        {
            this.type = type;
            this.IsMethod = isMethod;
            SetRangesToDefaultValues(valueType);
        }

        internal bool CanOverride
        {
            get
            {
                switch (SchemaType)
                {
                    case SchemaType.Int:
                    case SchemaType.UInt:
                    case SchemaType.Float:
                    case SchemaType.Vector2:
                    case SchemaType.Vector3:
                    case SchemaType.Color:
                    case SchemaType.Quaternion:
                        return true;
                }
                return IsMethod;
            }
        }

        internal bool IsRangeType()
        {
            return SchemaType == SchemaType.Int ||
                   SchemaType == SchemaType.UInt ||
                   (IsFloatType && FloatCompression == FloatCompression.FixedPoint);
        }

        internal static bool IsBitsBased(SchemaType type)
        {
            return type == SchemaType.Color || type == SchemaType.Quaternion;
        }

        internal static bool IsFloatBased(SchemaType type)
        {
            switch (type)
            {
                case SchemaType.Float:
                case SchemaType.Vector2:
                case SchemaType.Vector3:
                    return true;
            }
            return false;
        }

        internal void SetRange(long minRange, long maxRange)
        {
            this.minRange = minRange;
            this.maxRange = maxRange;
        }

        internal void SetSampleRate(float sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        internal void SetFloatCompression(FloatCompression floatCompression)
        {
            this.floatCompression = floatCompression;
        }

        internal virtual void CopyFrom(BindingArchetypeData other)
        {
            type = other.type;
            minRange = other.minRange;
            maxRange = other.maxRange;
            floatCompression = other.floatCompression;

            if (fields.Count > other.fields.Count)
            {
                fields.RemoveRange(other.fields.Count, fields.Count - other.fields.Count);
            }

            for (int i = 0; i < other.fields.Count; i++)
            {
                if (fields.Count > i)
                {
                    fields[i].CopyFrom(other.fields[i], this);
                }
                else
                {
                    fields.Add(new BindingLODStepData(other.fields[i], this));
                }
            }
        }

        internal bool Update(SchemaType type, Type valueType, int lodsteps)
        {
            bool changed = false;

            bool isNewType = type != this.type;

            if (isNewType)
            {
                changed = true;
                this.type = type;
                SetRangesToDefaultValues(valueType);
            }

            changed |= AddLODStep(lodsteps);

            return changed;
        }

        internal void SetRangesToDefaultValues(Type valueType)
        {
            if (valueType != null && ArchetypeMath.TryGetTypeLimits(valueType, out double typeMinRange, out double typeMaxRange))
            {
                minRange = (long)Math.Round(typeMinRange);
                maxRange = (long)Math.Round(typeMaxRange);
                return;
            }

            switch (SchemaType)
            {
                case SchemaType.Int:
                    minRange = (long)int.MinValue;
                    maxRange = (long)int.MaxValue;
                    break;
                case SchemaType.UInt:
                    minRange = (long)uint.MinValue;
                    maxRange = (long)uint.MaxValue;
                    break;
            }
        }

        public int GetTotalBitsOfLOD(int lodStep)
        {
            if (fields.Count > lodStep)
            {
                return fields[lodStep].TotalBits;
            }
            return 0;
        }

        internal BindingLODStepData GetLODstep(int lodStep)
        {
            if (fields == null || fields.Count == 0)
            {
                return null;
            }

            if (lodStep < fields.Count)
            {
                return fields[lodStep];
            }
            // This can happen on old data, if the user has done their own editing etc.
            return fields[fields.Count - 1];
        }

        internal bool AddLODStep(int lodStep)
        {
            bool changed = InstantiateFieldsList();

            while (fields.Count < lodStep)
            {
                changed = true;
                if (fields.Count > 0)
                {
                    BindingLODStepData newField = new BindingLODStepData(fields[fields.Count - 1], this);
                    newField.UpdateModel(this, fields.Count);
                    fields.Add(newField);
                }
                else
                {
                    BindingLODStepData newField = new BindingLODStepData(this, fields.Count);
                    fields.Add(newField);
                }
            }

            return changed;
        }

        private bool InstantiateFieldsList()
        {
            if (fields == null)
            {
                fields = new List<BindingLODStepData>();
                return true;
            }

            return false;
        }

        internal void RemoveLODLevel(int lodStep, int maxLods)
        {
            if (fields.Count > lodStep)
            {
                fields.RemoveAt(lodStep);
            }
            while (fields.Count > maxLods)
            {
                fields.RemoveAt(fields.Count - 1);
            }
        }

        internal void ResetValuesToDefault(Type bindingValueType, bool resetRanges, bool resetBitsAndPrecision)
        {
            if (resetRanges)
            {
                ResetRanges(bindingValueType);
            }

            if (resetBitsAndPrecision)
            {
                ResetBitsAndPrecision();
            }

            foreach (var field in Fields)
            {
                field.Verify(minRange, maxRange);
            }
        }

        internal (long min, long max) GetRangeByLODs()
        {
            long min = long.MinValue;
            long max = long.MaxValue;

            foreach (BindingLODStepData lod in fields)
            {
                (long lodMin, long lodMax) = ArchetypeMath.GetRangeByBitsAndPrecision(lod.Bits, lod.Precision);
                min = Math.Max(min, lodMin);
                max = Math.Min(max, lodMax);
            }

            return (min, max);
        }

        internal void ResetRanges(Type bindingValueType)
        {
            if (!IsRangeType())
            {
                minRange = 0;
                maxRange = 0;
                return;
            }

            if (IsFloatType && floatCompression == FloatCompression.FixedPoint)
            {
                (long min, long max) = ArchetypeMath.GetRangeByBitsAndPrecision(
                    BindingLODStepData.FLOAT_DEFAULT_BITS,
                    BindingLODStepData.FLOAT_DEFAULT_PRECISION);
                this.minRange = min;
                this.maxRange = max;
            }
            else
            {
                if (ArchetypeMath.TryGetTypeLimits(bindingValueType,
                        out double typeMinRange, out double typeMaxRange))
                {
                    this.minRange = (long)typeMinRange;
                    this.maxRange = (long)typeMaxRange;
                }
                else
                {
                    Debug.LogError($"Failed to get type limits for {bindingValueType}");
                }
            }
        }

        private void ResetBitsAndPrecision()
        {
            foreach (var field in Fields)
            {
                field.SetDefaultOverrides(SchemaType);
                field.Verify(minRange, maxRange);
            }
        }

        internal bool FixSerializedDataInFields()
        {
            if (InstantiateFieldsList())
            {
                return true;
            }

            bool changed = false;
            int index = 0;
            foreach (var field in fields)
            {
                if (field.SchemaType != SchemaType.Unknown && field.Bits == 0)
                {
                    changed = true;
                    field.UpdateModel(this, index, true);
                }

                index++;
            }

            return changed;
        }
    }
}
