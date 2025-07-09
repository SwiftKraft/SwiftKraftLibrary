namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;

    public class UIntBinding : ValueBinding<uint>
    {
        protected UIntBinding() { }
        public UIntBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override uint Value
        {
            get => (uint)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override uint ClampToRange(in uint value, long minRange, long maxRange)
        {
            if (value < minRange) return (uint)minRange;
            if (value > maxRange) return (uint)maxRange;
            return value;
        }

        protected override bool DiffersFrom(uint first, uint second)
        {
            return first != second;
        }
    }
}
