// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.NativeUtils
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Log;
    using Logger = Log.Logger;

    /// <summary>
    /// Searches for suspended threads in a Replication Server process and resumes them.
    /// Alleviates the issue of threads being suspended by the Steam.
    /// </summary>
    /// <remarks>Windows only.</remarks>
    public class ThreadResumer
    {
        private readonly int rsProcessId;
        private readonly CancellationToken cancellationToken;
        private readonly ThreadResumerSettings settings;
        private readonly Thread thread;
        private readonly HashSet<ulong> suspendedThreads = new();
        private readonly ulong[] threadsBuffer = new ulong[128];
        private readonly Logger logger = Log.GetLogger<ThreadResumer>();

        public ThreadResumer(
            int rsProcessId,
            CancellationToken cancellationToken,
            ThreadResumerSettings settings)
        {
            if (settings.SearchIntervalMs < 1)
            {
                throw new ArgumentException($"Search interval must be at least 1 ms. Was {settings.SearchIntervalMs}.");
            }

            this.rsProcessId = rsProcessId;
            this.cancellationToken = cancellationToken;
            this.settings = settings;

            if (!settings.Enabled)
            {
                return;
            }

            thread = new Thread(Run);
            thread.Start();
        }

        private void Run()
        {
            logger.Debug($"Started", ("rsPid", rsProcessId));

            while (!cancellationToken.IsCancellationRequested)
            {
                FindAndResumeSuspendedThreads();
                Thread.Sleep(Math.Max((int)settings.SearchIntervalMs, 1));
            }

            logger.Debug($"Finished", ("rsPid", rsProcessId));
        }

        internal int FindAndResumeSuspendedThreads()
        {
            var numSuspended = InteropAPI.TRFindSuspendedThreads(rsProcessId, threadsBuffer, (uint)threadsBuffer.Length, false, out var timeMs);
            for (var i = 0; i < numSuspended; i++)
            {
                var suspendedThreadID = threadsBuffer[i];

                var wasAlreadySuspended = suspendedThreads.Contains(suspendedThreadID);
                if (!wasAlreadySuspended)
                {
                    continue;
                }

                if (settings.WarnOnSuspension)
                {
                    logger.Warning(Warning.ThreadResumerSuspendedThreadFound, ("threadId", suspendedThreadID));
                }

                InteropAPI.TRResumeThread(suspendedThreadID);
            }

            suspendedThreads.Clear();
            for (var i = 0; i < numSuspended; i++)
            {
                suspendedThreads.Add(threadsBuffer[i]);
            }

            if (settings.LongSearchWarnThresholdMs > 0 && timeMs > settings.LongSearchWarnThresholdMs)
            {
                logger.Warning(Warning.ThreadResumerLongSearch, ("timeMs", timeMs));
            }

            return numSuspended;
        }
    }
}
