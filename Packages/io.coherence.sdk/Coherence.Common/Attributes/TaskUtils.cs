// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Utility methods related to <see cref="Task"/>.
    /// </summary>
    public static class TaskUtils
    {
        /// <summary>
        /// Gets the TaskScheduler from the current synchronization context, if one exists; otherwise, gets the <see cref="TaskScheduler.Current">current TaskScheduler</see>.
        /// <para>
        /// In Unity platforms, this is the UnitySynchronizationContext.
        /// </para>
        /// <remarks>
        /// This should be passed to <see cref="Task.ContinueWith(System.Action{Task}, TaskScheduler)"/>
        /// to have WebGL support.
        /// </remarks>
        /// </summary>
        public static readonly TaskScheduler Scheduler;

        static TaskUtils()
        {
            if (SynchronizationContext.Current is not null)
            {
                try
                {
                    Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                }
                catch (InvalidOperationException) // Handle "The current SynchronizationContext may not be used as a TaskScheduler".
                {
                    Scheduler = TaskScheduler.Current;
                }
            }
            else
            {
                Scheduler = TaskScheduler.Current;
            }
        }
    }
}
