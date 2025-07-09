// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook.Octet;
    using Common.Pooling;

    internal class PooledOutOctetStream : OutOctetStream, IPoolable
    {
        private readonly IPool<PooledOutOctetStream> streamPool;

        public PooledOutOctetStream(IPool<PooledOutOctetStream> streamPool, int streamCapacity) : base(streamCapacity)
        {
            this.streamPool = streamPool;
        }

        public void Return()
        {
            Reset();
            streamPool.Return(this);
        }

        public new void ResizeAndReset(int capacity)
        {
            base.ResizeAndReset(capacity);
        }
    }
}
