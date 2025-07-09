namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System.Reflection;
    using Entities;
    using UnityEngine;

    public class Vector2Binding : ValueBinding<Vector2>
    {
        protected Vector2Binding() { }
        public Vector2Binding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override Vector2 Value
        {
            get => (Vector2)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override Vector2 ClampToRange(in Vector2 value, long minRange, long maxRange)
        {
            return new Vector2(
                FloatBinding.ClampValueToRange(value.x, minRange, maxRange),
                FloatBinding.ClampValueToRange(value.y, minRange, maxRange)
            );
        }

        protected override bool DiffersFrom(Vector2 first, Vector2 second)
        {
            return first.x != second.x ||
                   first.y != second.y;
        }

        protected override Vector2 GetCompressedValue(Vector2 value)
        {
            var baseLod = archetypeData.GetLODstep(0);
            switch (baseLod.FloatCompression)
            {
                case FloatCompression.Truncated:
                    return new Vector2(
                        Brook.Utils.GetTruncatedFloatValue(value.x, baseLod.Bits),
                        Brook.Utils.GetTruncatedFloatValue(value.y, baseLod.Bits));
                case FloatCompression.FixedPoint:
                    return new Vector2(
                        (float)Brook.Utils.CompressFixedPoint(value.x, archetypeData.MinRange, archetypeData.MaxRange, baseLod.Precision),
                        (float)Brook.Utils.CompressFixedPoint(value.y, archetypeData.MinRange, archetypeData.MaxRange, baseLod.Precision));
                default:
                    return value;
            }
        }
    }
}
