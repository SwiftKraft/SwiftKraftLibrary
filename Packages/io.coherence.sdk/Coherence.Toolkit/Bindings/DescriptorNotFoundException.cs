namespace Coherence.Toolkit.Bindings
{
    using System;

    public class DescriptorNotFoundException : Exception
    {
        public DescriptorNotFoundException()
        {
        }

        public DescriptorNotFoundException(string message) : base(message)
        {
        }

        public DescriptorNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
