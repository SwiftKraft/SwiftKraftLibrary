// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;
    using System.Threading;

    public class ConcurrentStopwatch : IStopwatch
    {
        private readonly IStopwatch stopwatch;
        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        public ConcurrentStopwatch(IStopwatch stopwatch)
        {
            this.stopwatch = stopwatch;
        }

        public long ElapsedMilliseconds
        {
            get
            {
                rwLock.EnterReadLock();
                try
                {
                    return stopwatch.ElapsedMilliseconds;
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            }
        }

        public TimeSpan Elapsed
        {
            get
            {
                rwLock.EnterReadLock();
                try
                {
                    return stopwatch.Elapsed;
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            }
        }

        public void Start()
        {
            rwLock.EnterWriteLock();
            try
            {
                stopwatch.Start();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void Reset()
        {
            rwLock.EnterWriteLock();
            try
            {
                stopwatch.Reset();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void Restart()
        {
            rwLock.EnterWriteLock();
            try
            {
                stopwatch.Restart();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void Stop()
        {
            rwLock.EnterWriteLock();
            try
            {
                stopwatch.Stop();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }
}
