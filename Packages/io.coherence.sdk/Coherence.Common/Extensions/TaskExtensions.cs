// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for <see cref="Task"/>.
    /// </summary>
    internal static class TaskExtensions
    {
        /// <summary>
        /// Creates a continuation that executes asynchronously when the target Task completes.
        /// <remarks>
        /// Same as using <see cref="Task.ContinueWith(Action{Task}, CancellationToken)"/>, except that it uses the task scheduler
        /// from the current synchronization context when available (to have WebGL support).
        /// </remarks>>
        /// </summary>
        /// <param name="task"> Task whose completion to wait for. </param>
        /// <param name="action"> Delegate to execute when the task completes. </param>
        /// <param name="cancellationToken"> Token for canceling the continuation action. </param>
        public static void Then([DisallowNull] this Task task, [DisallowNull] Action action, CancellationToken cancellationToken = default)
            => task.ContinueWith(_ => action(), cancellationToken, TaskContinuationOptions.None, TaskUtils.Scheduler);

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target Task completes.
        /// <remarks>
        /// Same as using <see cref="Task.ContinueWith(Action{Task}, CancellationToken)"/>, except that it uses the task scheduler
        /// from the current synchronization context when available (to have WebGL support).
        /// </remarks>>
        /// </summary>
        /// <param name="task"> Task whose completion to wait for. </param>
        /// <param name="action"> Delegate to execute when the task completes. </param>
        /// <param name="taskContinuationOptions"> Options for when the continuation is scheduled and how it behaves. </param>
        /// <param name="cancellationToken"> Token for canceling the continuation action. </param>
        public static void Then([DisallowNull] this Task task, [DisallowNull] Action action, TaskContinuationOptions taskContinuationOptions, CancellationToken cancellationToken = default)
            => task.ContinueWith(_ => action(), cancellationToken, taskContinuationOptions, TaskUtils.Scheduler);

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target Task completes.
        /// <remarks>
        /// Same as using <see cref="Task.ContinueWith(Action{Task}, CancellationToken)"/>, except that it uses the task scheduler
        /// from the current synchronization context when available (to have WebGL support).
        /// </remarks>>
        /// </summary>
        /// <param name="task"> Task whose completion to wait for. </param>
        /// <param name="action"> Delegate to execute when the task completes. </param>
        /// <param name="cancellationToken"> Token for canceling the continuation action. </param>
        public static void Then([DisallowNull] this Task task, [DisallowNull] Action<Task> action, CancellationToken cancellationToken = default)
            => task.ContinueWith(action, cancellationToken, TaskContinuationOptions.None, TaskUtils.Scheduler);

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target Task completes.
        /// <remarks>
        /// Same as using <see cref="Task.ContinueWith{TResult}(Func{Task, TResult}, CancellationToken)"/>, except that it uses the task scheduler
        /// from the current synchronization context when available (to have WebGL support).
        /// </remarks>>
        /// </summary>
        /// <param name="task"> Task whose completion to wait for. </param>
        /// <param name="continuationAsync"> Delegate to execute when the task completes which returns a Task as a result. </param>
        /// <param name="cancellationToken"> Token for canceling the continuation action. </param>
        /// <returns>
        /// A new continuation <see cref="Task{TResult}"/>.
        /// </returns>
        public static Task<TResult> Then<TResult>([DisallowNull] this Task task, [DisallowNull] Func<Task, TResult> continuationAsync, CancellationToken cancellationToken = default)
            => task.ContinueWith(continuationAsync, cancellationToken, TaskContinuationOptions.None, TaskUtils.Scheduler);

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target Task completes.
        /// <remarks>
        /// Same as using <see cref="Task.ContinueWith(Action{Task}, CancellationToken)"/>, except that it uses the task scheduler
        /// from the current synchronization context when available (to have WebGL support).
        /// </remarks>>
        /// </summary>
        /// <param name="task"> Task whose completion to wait for. </param>
        /// <param name="action"> Delegate to execute when the task completes. </param>
        /// <param name="taskContinuationOptions"> Options for when the continuation is scheduled and how it behaves. </param>
        /// <param name="cancellationToken"> Token for canceling the continuation action. </param>
        public static void Then([DisallowNull] this Task task, [DisallowNull] Action<Task> action, TaskContinuationOptions taskContinuationOptions, CancellationToken cancellationToken = default)
            => task.ContinueWith(action, cancellationToken, taskContinuationOptions, TaskUtils.Scheduler);

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target Task completes.
        /// <remarks>
        /// Same as using <see cref="Task.ContinueWith(Action{Task}, CancellationToken)"/>, except that it uses the task scheduler
        /// from the current synchronization context when available (to have WebGL support).
        /// </remarks>>
        /// </summary>
        /// <param name="task"> Task whose completion to wait for. </param>
        /// <param name="action"> Delegate to execute when the task completes. </param>
        /// <param name="cancellationToken"> Token for canceling the continuation action. </param>
        public static void Then<TResult>([DisallowNull] this Task<TResult> task, [DisallowNull] Action<Task<TResult>> action, CancellationToken cancellationToken = default)
            => task.ContinueWith(action, cancellationToken, TaskContinuationOptions.None, TaskUtils.Scheduler);

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target Task completes.
        /// <remarks>
        /// Same as using <see cref="Task.ContinueWith(Action{Task}, CancellationToken)"/>, except that it uses the task scheduler
        /// from the current synchronization context when available (to have WebGL support).
        /// </remarks>>
        /// </summary>
        /// <param name="task"> Task whose completion to wait for. </param>
        /// <param name="action"> Delegate to execute when the task completes. </param>
        /// <param name="taskContinuationOptions"> Options for when the continuation is scheduled and how it behaves. </param>
        /// <param name="cancellationToken"> Token for canceling the continuation action. </param>
        public static void Then<TResult>([DisallowNull] this Task<TResult> task, [DisallowNull] Action<Task<TResult>> action, TaskContinuationOptions taskContinuationOptions, CancellationToken cancellationToken = default)
            => task.ContinueWith(action, cancellationToken, taskContinuationOptions, TaskUtils.Scheduler);
    }
}
