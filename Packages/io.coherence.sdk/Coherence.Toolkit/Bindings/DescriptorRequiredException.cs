namespace Coherence.Toolkit.Bindings
{
    using System;

    public class DescriptorRequiredException : Exception
    {
        public DescriptorRequiredException()
        {
        }

        public DescriptorRequiredException(string message) : base(message)
        {
        }

        public DescriptorRequiredException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
