// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;
    using System.Diagnostics;

    public interface IStopwatch
    {
        /// <inheritdoc cref="Stopwatch.ElapsedMilliseconds"/>
        long ElapsedMilliseconds { get; }

        /// <inheritdoc cref="Stopwatch.Elapsed"/>
        TimeSpan Elapsed { get; }

        /// <inheritdoc cref="Stopwatch.Start"/>
        void Start();

        /// <inheritdoc cref="Stopwatch.Reset"/>
        void Reset();

        /// <inheritdoc cref="Stopwatch.Restart"/>
        void Restart();

        /// <inheritdoc cref="Stopwatch.Stop"/>
        void Stop();
    }
}
