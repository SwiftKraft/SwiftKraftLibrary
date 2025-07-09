namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class LongBinding : ValueBinding<long>
    {
        protected LongBinding() { }
        public LongBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override long Value
        {
            get => (long)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(long first, long second)
        {
            return first != second;
        }
    }
}
