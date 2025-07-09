// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling
{
    using System;
    using System.Collections.Generic;

    internal class ListPool<T>
    {
        private readonly Pool<PooledList<T>> pool;

        public ListPool(int initialListCapacity = 0, int poolPrefillSize = 0)
        : this(null, initialListCapacity, poolPrefillSize)
        {

        }

        public ListPool(
            Func<IPool<PooledList<T>>, PooledList<T>> objectGenerator = null,
            int initialListCapacity = 0,
            int poolPrefillSize = 0)
        {
            pool = Pool<PooledList<T>>
                .Builder(objectGenerator ?? (pool => new PooledList<T>(pool, initialListCapacity)))
                .Prefill(poolPrefillSize)
                .WithReusables()
                .Build();
        }

        public void Return(List<T> list)
        {
            if (list is not PooledList<T> pooledList)
            {
                throw new ArgumentException($"The list must be of type {nameof(PooledList<T>)}", nameof(list));
            }

            pool.Return(pooledList);
        }

        public PooledList<T> Rent()
        {
            return pool.Rent();
        }
    }
}
