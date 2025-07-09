// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;
    using System.Diagnostics;

    public class SystemStopwatch : IStopwatch
    {
        public static IStopwatch StartNew() => new SystemStopwatch();

        private readonly Stopwatch stopwatch = new Stopwatch();

        /// <inheritdoc/>
        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

        /// <inheritdoc/>
        public TimeSpan Elapsed => stopwatch.Elapsed;

        /// <inheritdoc/>
        public void Start() => stopwatch.Start();

        /// <inheritdoc/>
        public void Stop() => stopwatch.Stop();

        /// <inheritdoc/>
        public void Reset() => stopwatch.Reset();

        /// <inheritdoc/>
        public void Restart() => stopwatch.Restart();
    }
}
