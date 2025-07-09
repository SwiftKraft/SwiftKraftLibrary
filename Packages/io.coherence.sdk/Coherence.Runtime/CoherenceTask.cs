// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    /// <summary>
    /// Represents an asynchronous operation with a result of type <see typeparamref="TResult"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Async methods can use <see langword="await"/> to wait until the operation finishes.
    /// </para>
    /// <para>
    /// Coroutines can use <see langword="yield"/> to wait until the operation finishes.
    /// </para>
    /// <para>
    /// <see cref="ContinueWith(Action, TaskContinuationOptions)"/> can also be used to perform an action after the operation has completed.
    /// </para>
    /// <para>
    /// Coherence tasks do not throw exceptions if they are cancelled;
    /// you can determine if an operation has been canceled using <see cref="CoherenceTask.IsCanceled"/>
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult"> Type of object returned if the operation succeeds. </typeparam>
    public sealed class CoherenceTask<TResult> : CoherenceTask
    {
        /// <summary>
        /// The result of the operation, if it has <see cref="CoherenceTask.IsCompletedSuccessfully">
        /// completed successfully</see>; otherwise, the default value of <see cref="TResult"/>.
        /// </summary>
        [MaybeNull]
        public TResult Result => task.IsCompletedSuccessfully ? task.Result : default;

        internal new Task<TResult> task => (Task<TResult>)base.task;

        internal CoherenceTask(Task<TResult> task) : base(task) { }
        internal CoherenceTask(Task<TResult> task, CancellationToken cancellationToken) : base(task, cancellationToken) { }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="CoherenceTask.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public new CoherenceTask<TResult> ContinueWith([DisallowNull] Action action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            if (IsCompleted)
            {
                action();
                return this;
            }

            task.ContinueWith(_ => action(), cancellationToken, continuationOptions, TaskUtils.Scheduler);
            return this;
        }

        /// <summary>
        /// Specify an action to perform if the operation completes successfully
        /// (<see cref="CoherenceTask.IsCompletedSuccessfully"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has completed successfully.
        /// </param>
        public CoherenceTask<TResult> OnSuccess([DisallowNull] Action<TResult> action)
        {
            if (IsCompletedSuccessfully)
            {
                action(Result);
                return this;
            }

            task.ContinueWith(completedTask => action(completedTask.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            return this;
        }

        /// <inheritdoc cref="CoherenceTask.GetAwaiter()"/>
        public new TaskAwaiter<CoherenceTask<TResult>> GetAwaiter() => GetAwaiter(this);

        public override string ToString()
        {
            if (!IsCompleted)
            {
                return task.Status.ToString();
            }

            return "Result: " + (task.Result is { } result ? result.ToString() : "null");
        }

        public static implicit operator TResult(CoherenceTask<TResult> operation) => operation.Result;
    }

    /// <summary>
    /// Represents an asynchronous operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Async methods can <see langword="await"/> to wait until the operation finishes.
    /// </para>
    /// <para>
    /// Coroutines can <see langword="yield"/> to wait until the operation finishes.
    /// </para>
    /// <para>
    /// Coherence tasks do not throw exceptions if they are cancelled;
    /// you can determine if an operation has been canceled using <see cref="IsCanceled"/>
    /// instead.
    /// </para>
    /// </remarks>
    public class CoherenceTask
#if UNITY
        : UnityEngine.CustomYieldInstruction
#endif
    {
        /// <summary>
        /// Gets whether the operation has completed.
        /// </summary>
        /// <remarks>
        /// <see langword="true"/> if the operation <see cref="IsCompletedSuccessfully">completed successfully</see>,
        /// or was <see cref="IsCanceled">canceled</see>; otherwise, <see langword="false"/>.
        /// </remarks>
        public bool IsCompleted => task.IsCompleted;

        /// <summary>
        /// Gets whether the operation has completed successfully.
        /// </summary>
        public bool IsCompletedSuccessfully => task.IsCompletedSuccessfully;

        /// <summary>
        /// Gets whether this operation has completed execution due to being canceled.
        /// </summary>
        public bool IsCanceled => task.IsCanceled;

        public
#if UNITY
            sealed override
#endif
            bool keepWaiting => !task.IsCompleted && !cancellationToken.IsCancellationRequested;

        protected internal readonly Task task;
        internal readonly CancellationToken cancellationToken;

        protected CoherenceTask(Task task) : this(task,
#if UNITY && UNITY_2022_2_OR_NEWER
            UnityEngine.Application.exitCancellationToken)
#else
            default)
#endif
        { }

        protected CoherenceTask(Task task, CancellationToken cancellationToken)
        {
            this.task = task;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Generates an object that can be awaited to wait for the completion of the operation.
        /// </summary>
        /// Awaiting the operation never results in an exception being thrown.
        /// You can use <see cref="IsCanceled"/> to determine if the operation was canceled.
        /// <param name="operation"> The operation being <see langword="await">awaited</see>. </param>
        /// <typeparam name="TOperation"> Type of the operation being awaited. </typeparam>
        /// <returns>
        /// A new TaskAwaiter that completes when the operation has completed successfully,
        /// or been cancelled, and returns the operation itself as the result.
        /// </returns>
        private protected TaskAwaiter<TOperation> GetAwaiter<TOperation>(TOperation operation) where TOperation : CoherenceTask
        {
            var taskCompletionSource = new TaskCompletionSource<TOperation>(operation.cancellationToken);
            operation.task.ContinueWith(SetResult, default, TaskContinuationOptions.ExecuteSynchronously, TaskUtils.Scheduler);
            return taskCompletionSource.Task.GetAwaiter();

            void SetResult(Task completedTask)
            {
                // NOTE: Intentionally not using SetCanceled, even if the task is canceled,
                // so that awaiting the operation never results in an exception being thrown.
                taskCompletionSource.SetResult(operation);
            }
        }

        /// <summary>
        /// Gets an object that can be used to <see langword="await"/> for this operation to complete.
        /// </summary>
        /// <remarks>
        /// Awaiting the result of this method never causes an exception to be thrown. You can use
        /// <see cref="IsCanceled"/> to determine if the operation has been canceled instead.
        /// </remarks>
        /// <returns> A new task awaiter instance. </returns>
        public TaskAwaiter<CoherenceTask> GetAwaiter() => GetAwaiter(this);

        /// <summary>
        /// Specify an action to perform after the operation has completed (<see cref="IsCompleted"/> is
        /// <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public CoherenceTask ContinueWith([DisallowNull] Action action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            if (IsCompleted)
            {
                action();
                return this;
            }

            task.ContinueWith(_ => action(), continuationOptions);
            return this;
        }

        /// <summary>
        /// Specify an action to perform if the operation completes successfully (<see cref="IsCompletedSuccessfully"/>
        /// is <see langword="true"/>.
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has completed successfully.
        /// </param>
        public CoherenceTask OnSuccess([DisallowNull] Action action)
        {
            if (IsCompletedSuccessfully)
            {
                action();
                return this;
            }

            task.ContinueWith(_ => action(), TaskContinuationOptions.OnlyOnRanToCompletion);
            return this;
        }

        public override string ToString() => task.Status.ToString();
        public static implicit operator Task(CoherenceTask operation) => operation.task;
    }
}
