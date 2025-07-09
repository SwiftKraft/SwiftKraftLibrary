namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class SByteBinding : ValueBinding<sbyte>
    {
        protected SByteBinding() { }
        public SByteBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override sbyte Value
        {
            get => (sbyte)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(sbyte first, sbyte second)
        {
            return first != second;
        }
    }
}
