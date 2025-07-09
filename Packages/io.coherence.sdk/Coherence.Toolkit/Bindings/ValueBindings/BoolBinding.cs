namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using Entities;
    using UnityEngine;

    public class BoolBinding : ValueBinding<bool>
    {
        protected BoolBinding() { }
        public BoolBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override bool Value
        {
            get => (bool)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(bool first, bool second)
        {
            return first != second;
        }
    }
}
