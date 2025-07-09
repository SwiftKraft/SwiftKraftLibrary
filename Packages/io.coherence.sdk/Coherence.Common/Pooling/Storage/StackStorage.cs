// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling.Storage
{
    using System.Collections.Generic;

    internal class StackStorage<T> : IPoolStorage<T>
    {
        private readonly Stack<T> stack = new();

        public bool TryTake(out T item) => stack.TryPop(out item);
        public void Add(T item) => stack.Push(item);
    }
}
