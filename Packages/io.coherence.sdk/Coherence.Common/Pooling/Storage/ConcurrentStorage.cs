// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling.Storage
{
    using System.Collections.Concurrent;

    internal class ConcurrentStorage<T> : IPoolStorage<T>
    {
        private readonly ConcurrentBag<T> bag = new();

        public bool TryTake(out T item) => bag.TryTake(out item);
        public void Add(T item) => bag.Add(item);
    }
}
