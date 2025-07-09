// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling.Storage
{
    internal interface IPoolStorage<T>
    {
        bool TryTake(out T item);
        void Add(T item);
    }
}
