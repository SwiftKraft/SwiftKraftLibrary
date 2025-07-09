// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook;
    using Common.Pooling;

    internal static class PooledStreamExtensions
    {
        public static void ReturnIfPoolable(this IOutOctetStream stream)
        {
            if (stream is IPoolable poolable)
            {
                poolable.Return();
            }
        }

        public static void ReturnIfPoolable(this IInOctetStream stream)
        {
            if (stream is IPoolable poolable)
            {
                poolable.Return();
            }
        }
    }
}
