namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class ShortBinding : ValueBinding<short>
    {
        protected ShortBinding() { }
        public ShortBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override short Value
        {
            get => (short)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(short first, short second)
        {
            return first != second;
        }
    }
}
