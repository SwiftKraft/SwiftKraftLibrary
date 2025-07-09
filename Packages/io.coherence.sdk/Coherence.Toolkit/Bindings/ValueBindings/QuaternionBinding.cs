namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System.Reflection;
    using Entities;
    using UnityEngine;

    public class QuaternionBinding : ValueBinding<Quaternion>
    {
        protected QuaternionBinding() { }
        public QuaternionBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override Quaternion Value
        {
            get => (Quaternion)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(Quaternion first, Quaternion second)
        {
            return first.x != second.x
                   || first.y != second.y
                   || first.z != second.z
                   || first.w != second.w;
        }

        protected override Quaternion GetCompressedValue(Quaternion value)
        {
            var baseLod = archetypeData.GetLODstep(0);
            return Brook.Utils.CompressQuaternion(value.ToCoreQuaternion(), baseLod.Bits).ToUnityQuaternion();
        }
    }
}
