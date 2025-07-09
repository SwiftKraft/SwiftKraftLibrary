// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling
{
    internal interface IPool<T>
    {
        T Rent();
        void Return(T item);
    }
}
