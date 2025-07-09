// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling
{
    using System;
    using System.Collections.Generic;

    internal class PooledList<T> : List<T>, IPoolable, IDisposable, IReusable
    {
        private IPool<PooledList<T>> pool;

        public PooledList(IPool<PooledList<T>> pool) => this.pool = pool;
        public PooledList(IPool<PooledList<T>> pool, IEnumerable<T> collection) : base(collection) => this.pool = pool;
        public PooledList(IPool<PooledList<T>> pool, int capacity) : base(capacity) => this.pool = pool;

        public void Return() => pool.Return(this);
        public void Dispose() => Return();
        public void ResetState() => Clear();
    }
}
