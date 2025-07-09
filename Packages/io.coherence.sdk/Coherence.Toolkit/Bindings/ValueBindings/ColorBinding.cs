namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using Entities;
    using UnityEngine;

    public class ColorBinding : ValueBinding<Color>
    {
        protected ColorBinding() { }
        public ColorBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override Color Value
        {
            get => (Color)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(Color first, Color second)
        {
            return first != second;
        }

        protected override Color GetCompressedValue(Color value)
        {
            var baseLod = archetypeData.GetLODstep(0);
            return new Color(
                (float)Brook.Utils.CompressFixedPoint(value.r, 0, 1, baseLod.Precision),
                (float)Brook.Utils.CompressFixedPoint(value.g, 0, 1, baseLod.Precision),
                (float)Brook.Utils.CompressFixedPoint(value.b, 0, 1, baseLod.Precision),
                (float)Brook.Utils.CompressFixedPoint(value.a, 0, 1, baseLod.Precision));
        }
    }
}
