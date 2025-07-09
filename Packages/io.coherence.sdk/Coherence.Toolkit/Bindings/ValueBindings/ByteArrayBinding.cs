namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using Entities;
    using UnityEngine;
    using Utils;

    public class ByteArrayBinding : ValueBinding<byte[]>
    {
        protected ByteArrayBinding() { }
        public ByteArrayBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override byte[] Value
        {
            get => (byte[])GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(byte[] first, byte[] second)
        {
            return first.DiffersFrom(second);
        }
    }
}
