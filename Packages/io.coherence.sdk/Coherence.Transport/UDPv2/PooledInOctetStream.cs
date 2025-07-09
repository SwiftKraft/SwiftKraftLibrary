// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using System;
    using Brook.Octet;
    using Common.Pooling;

    internal class PooledInOctetStream : InOctetStream, IPoolable
    {
        private readonly IPool<PooledInOctetStream> streamPool;

        public PooledInOctetStream(IPool<PooledInOctetStream> streamPool, int bufferSize = 0)
            : base(bufferSize)
        {
            this.streamPool = streamPool;
        }

        public void Return()
        {
            streamPool.Return(this);
        }

        public void Reset(ReadOnlySpan<byte> data)
        {
            ResetAndWrite(data);
        }
    }
}
