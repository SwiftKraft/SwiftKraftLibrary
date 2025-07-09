namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class CharBinding : ValueBinding<char>
    {
        protected CharBinding() { }
        public CharBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override char Value
        {
            get => (char)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(char first, char second)
        {
            return first != second;
        }
    }
}
