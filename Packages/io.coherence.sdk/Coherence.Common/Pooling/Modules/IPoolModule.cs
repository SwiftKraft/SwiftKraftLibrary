// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling.Modules
{
    internal interface IPoolModule<T>
    {
        void OnRent(in T item);
        void OnReturn(in T item);
    }
}
