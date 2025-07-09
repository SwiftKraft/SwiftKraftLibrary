namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using Entities;
    using UnityEngine;
    using Utils;

    public class FloatBinding : ValueBinding<float>
    {
        protected FloatBinding() { }
        public FloatBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override float Value
        {
            get => (float)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override float ClampToRange(in float value, long minRange, long maxRange)
        {
            return ClampValueToRange(value, minRange, maxRange);
        }

        protected override bool DiffersFrom(float first, float second)
        {
            return first != second;
        }

        protected override float GetCompressedValue(float value)
        {
            var baseLod = archetypeData.GetLODstep(0);
            switch (baseLod.FloatCompression)
            {
                case FloatCompression.Truncated:
                    return Brook.Utils.GetTruncatedFloatValue(value, baseLod.Bits);
                case FloatCompression.FixedPoint:
                    return (float)Brook.Utils.CompressFixedPoint(value, archetypeData.MinRange, archetypeData.MaxRange, baseLod.Precision);
                default:
                    return value;
            }
        }

        internal static float ClampValueToRange(in float value, long minRange, long maxRange)
        {
            if (value < minRange) return minRange;
            if (value > maxRange) return maxRange;
            return value;
        }
    }
}
