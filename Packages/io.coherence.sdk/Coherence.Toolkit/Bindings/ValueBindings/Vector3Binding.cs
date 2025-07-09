namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System.Reflection;
    using Entities;
    using UnityEngine;

    public class Vector3Binding : ValueBinding<Vector3>
    {
        protected Vector3Binding() { }
        public Vector3Binding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override Vector3 Value
        {
            get => (Vector3)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override Vector3 ClampToRange(in Vector3 value, long minRange, long maxRange)
        {
            return new Vector3(
                FloatBinding.ClampValueToRange(value.x, minRange, maxRange),
                FloatBinding.ClampValueToRange(value.y, minRange, maxRange),
                FloatBinding.ClampValueToRange(value.z, minRange, maxRange)
            );
        }

        protected override bool DiffersFrom(Vector3 first, Vector3 second)
        {
            return first.x != second.x ||
                   first.y != second.y ||
                   first.z != second.z;
        }

        protected override Vector3 GetCompressedValue(Vector3 value)
        {
            var baseLod = archetypeData.GetLODstep(0);
            switch (baseLod.FloatCompression)
            {
                case FloatCompression.Truncated:
                    return new Vector3(
                        Brook.Utils.GetTruncatedFloatValue(value.x, baseLod.Bits),
                        Brook.Utils.GetTruncatedFloatValue(value.y, baseLod.Bits),
                        Brook.Utils.GetTruncatedFloatValue(value.z, baseLod.Bits));
                case FloatCompression.FixedPoint:
                    return new Vector3(
                        (float)Brook.Utils.CompressFixedPoint(value.x, archetypeData.MinRange, archetypeData.MaxRange, baseLod.Precision),
                        (float)Brook.Utils.CompressFixedPoint(value.y, archetypeData.MinRange, archetypeData.MaxRange, baseLod.Precision),
                        (float)Brook.Utils.CompressFixedPoint(value.z, archetypeData.MinRange, archetypeData.MaxRange, baseLod.Precision));
                default:
                    return value;
            }
        }
    }
}
