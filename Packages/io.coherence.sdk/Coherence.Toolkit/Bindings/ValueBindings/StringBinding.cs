namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System.Reflection;
    using Entities;
    using UnityEngine;

    public class StringBinding : ValueBinding<string>
    {
        protected StringBinding() { }
        public StringBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override string Value
        {
            get => (string)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(string first, string second)
        {
            return first != second;
        }
    }
}
