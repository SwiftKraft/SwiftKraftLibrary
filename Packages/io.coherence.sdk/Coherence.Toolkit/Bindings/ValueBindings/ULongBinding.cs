namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class ULongBinding : ValueBinding<ulong>
    {
        protected ULongBinding() { }
        public ULongBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override ulong Value
        {
            get => (ulong)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(ulong first, ulong second)
        {
            return first != second;
        }
    }
}
