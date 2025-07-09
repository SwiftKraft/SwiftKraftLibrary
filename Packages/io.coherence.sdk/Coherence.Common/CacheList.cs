// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;
    using System.Collections.Generic;

    internal class CacheList<T> : List<T>, IDisposable
    {
        public CacheList()
        {
        }

        public CacheList(IEnumerable<T> collection) : base(collection)
        {
        }

        public CacheList(int capacity) : base(capacity)
        {
        }

        public void Dispose() => Clear();
    }
}
