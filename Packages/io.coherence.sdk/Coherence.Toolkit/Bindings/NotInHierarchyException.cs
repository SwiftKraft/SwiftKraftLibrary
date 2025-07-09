namespace Coherence.Toolkit.Bindings
{
    using System;

    public class NotInHierarchyException : Exception
    {
        public NotInHierarchyException()
        {
        }

        public NotInHierarchyException(string message) : base(message)
        {
        }

        public NotInHierarchyException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
