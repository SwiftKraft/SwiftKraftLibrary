namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class DoubleBinding : ValueBinding<double>
    {
        protected DoubleBinding() { }
        public DoubleBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override double Value
        {
            get => (double)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(double first, double second)
        {
            return first != second;
        }

        protected override double GetCompressedValue(double value)
        {
            var baseLod = archetypeData.GetLODstep(0);
            switch (baseLod.FloatCompression)
            {
                case FloatCompression.Truncated:
                    return Brook.Utils.GetTruncatedDoubleValue(value, baseLod.Bits);
                case FloatCompression.FixedPoint:
                    return Brook.Utils.CompressFixedPoint(value, archetypeData.MinRange, archetypeData.MaxRange, baseLod.Precision);
                default:
                    return value;
            }
        }
    }
}
