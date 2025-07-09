namespace Coherence.Toolkit.Bindings
{
    using System;

    public class NonBindableException : Exception
    {
        public NonBindableException()
        {
        }

        public NonBindableException(string message) : base(message)
        {
        }

        public NonBindableException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
