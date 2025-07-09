// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Coherence.Common
{
    using System;

    public class AsyncQueue<T>
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

        public void Enqueue(T item)
        {
            queue.Enqueue(item);
            semaphore.Release();
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await semaphore.WaitAsync(cancellationToken);

                if (queue.TryDequeue(out T item))
                {
                    return item;
                }
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }
}
