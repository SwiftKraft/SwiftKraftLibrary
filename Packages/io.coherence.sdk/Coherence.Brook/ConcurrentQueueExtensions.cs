// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Client
{
    using System.Collections.Concurrent;

    public static class ConcurrentQueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out T _))
            {
                // intentionally do nothing
            }
        }
    }
}
