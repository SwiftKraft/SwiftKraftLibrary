// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Common;
    using Log;
    using Runtime;
    using Utils;

    /// <summary>
    /// Represents an asynchronous cloud operation related to a <see cref="PlayerAccount"/> that has logged in to coherence Cloud.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see langword="async"/> method can <see langword="await"/> the <see cref="PlayerAccountOperation{TResult}"/> to wait until the operation has completed.
    /// </para>
    /// <para>
    /// Similarly, a <see cref="UnityEngine.Coroutine"/> can <see langword="yield"/> the <see cref="PlayerAccountOperation{TResult}"/> to wait for it to complete.
    /// </para>
    /// <para>
    /// <see cref="ContinueWith(Action, TaskContinuationOptions)"/> can also be used to perform an action after the operation has completed.
    /// </para>
    /// <para>
    /// <see cref="OnSuccess(Action{TResult})"/> and <see cref="OnFail(Action{PlayerAccountOperationError})"/> can be used to perform different actions based on
    /// whether the operation was successful or not.
    /// </para>
    /// <para>
    /// If <see cref="PlayerAccountOperation{TResult}.HasFailed"/> is <see langword="true"/>, then the operation has failed. If this is the case, then
    /// <see cref="PlayerAccountOperation{TResult}.Error"/> will be non-<see langword="null"/> and contain additional information about the error.
    /// </para>
    /// <para>
    /// If a <see cref="PlayerAccountOperation{TResult}"/> fails, and the error is not handled in any way (<see cref="PlayerAccountOperation{TResult}.HasFailed"/> is not
    /// checked, <see cref="PlayerAccountOperation{TResult}.Error"/> is not accessed, <see cref="OnFail"/> is not used, etc.), then the error will automatically
    /// be logged to the Console at some point (whenever the garbage collector releases the <see cref="PlayerAccountOperationError"/> from memory).
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult"> Type of object returned if the operation succeeds. </typeparam>
    public sealed class PlayerAccountOperation<TResult> : CloudOperation<TResult, PlayerAccountOperationError>
    {
        internal PlayerAccountOperation(Task<TResult> task) : base(task) { }
        internal PlayerAccountOperation(PlayerAccountErrorType errorType, Error error, string message = null) : this(Task.FromException<TResult>(new PlayerAccountOperationException(errorType, error, message))) { }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="PlayerAccountOperation{TResult}.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public new PlayerAccountOperation<TResult> ContinueWith([DisallowNull] Action action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            base.ContinueWith(action, continuationOptions);
            return this;
        }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="PlayerAccountOperation{TResult}.IsCompleted"/> becomes <see langword="true"/>).
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public PlayerAccountOperation<TResult> ContinueWith([DisallowNull] Action<PlayerAccountOperation<TResult>> action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            if (IsCompleted)
            {
                if (!continuationOptions.HasFlag(TaskContinuationOptions.NotOnFaulted))
                {
                    MarkErrorAsObserved();
                }

                action(this);
                return this;
            }

            task.ContinueWith(_ =>
            {
                if (!continuationOptions.HasFlag(TaskContinuationOptions.NotOnFaulted))
                {
                    MarkErrorAsObserved();
                }

                action(this);
            }, cancellationToken, continuationOptions, TaskUtils.Scheduler);
            return this;
        }

        /// <summary>
        /// Specify an action to perform if the operation completes successfully
        /// (<see cref="PlayerAccountOperation{TResult}.IsCompletedSuccessfully"/> becomes <see langword="true"/>).
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has completed successfully.
        /// </param>
        public new PlayerAccountOperation<TResult> OnSuccess([DisallowNull] Action<TResult> action)
        {
            base.OnSuccess(action);
            return this;
        }

        /// <summary>
        /// Specify an action to perform if the operation fails (<see cref="PlayerAccountOperation{TResult}.HasFailed"/> becomes
        /// <see langword="true"/>.
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has failed.
        /// </param>
        public new PlayerAccountOperation<TResult> OnFail([DisallowNull] Action<PlayerAccountOperationError> action)
        {
            base.OnFail(action);
            return this;
        }

        /// <inheritdoc cref="CloudOperation{TResult, PlayerAccountOperationError}.GetAwaiter()"/>
        public new TaskAwaiter<PlayerAccountOperation<TResult>> GetAwaiter() => GetAwaiter(this);

        internal override PlayerAccountOperationError CreateError([DisallowNull] Exception exception, object args = null)
            => exception.TryExtract(out PlayerAccountOperationException playerAccountOperationException)
            ? new(playerAccountOperationException)
            : exception.TryExtract(out RequestException requestException)
            ? new(PlayerAccountErrorType.ServerError, Coherence.Log.Error.RuntimeServerError, requestException.UserMessage, errorHasBeenObserved)
            : new(PlayerAccountErrorType.InternalException, Coherence.Log.Error.RuntimeInternalException, exception.ToString());
    }

    /// <summary>
    /// Represents an asynchronous cloud operation related to a <see cref="PlayerAccount"/> that has logged in to coherence Cloud.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see langword="async"/> method can <see langword="await"/> the <see cref="PlayerAccountOperation{TResult}"/> to wait until the operation has completed.
    /// </para>
    /// <para>
    /// Similarly, a <see cref="UnityEngine.Coroutine"/> can <see langword="yield"/> the <see cref="PlayerAccountOperation{TResult}"/> to wait for it to complete.
    /// </para>
    /// <para>
    /// <see cref="ContinueWith(Action, TaskContinuationOptions)"/> can also be used to perform an action after the operation has completed.
    /// </para>
    /// <para>
    /// <see cref="CloudOperation{TResult, PlayerAccountOperationError}.OnSuccess(Action)"/> and <see cref="CloudOperation{TResult, PlayerAccountOperationError}.OnFail(Action{PlayerAccountOperationError})"/> can be used to perform different actions based on
    /// whether the operation was successful or not.
    /// </para>
    /// <para>
    /// If <see cref="PlayerAccountOperation.HasFailed"/> is <see langword="true"/>, then the operation has failed. If this is the case, then
    /// <see cref="PlayerAccountOperation{TResult}.Error"/> will be non-<see langword="null"/> and contain additional information about the error.
    /// </para>
    /// <para>
    /// If a <see cref="PlayerAccountOperation"/> fails, and the error is not handled in any way (<see cref="PlayerAccountOperation.HasFailed"/> is not
    /// checked, <see cref="PlayerAccountOperation.Error"/> is not accessed, <see cref="CloudOperation{TResult, PlayerAccountOperationError}.OnFail"/> is not used, etc.), then the error will automatically
    /// be logged to the Console at some point (whenever the garbage collector releases the <see cref="PlayerAccountOperationError"/> from memory).
    /// </para>
    /// </remarks>
    public sealed class PlayerAccountOperation : CloudOperation<PlayerAccountOperationError>
    {
        internal PlayerAccountOperation(Task task) : base(task) { }
        internal PlayerAccountOperation(PlayerAccountErrorType errorType, Error error, string message = null) : this(Task.FromException(new PlayerAccountOperationException(errorType, error, message))) { }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="PlayerAccountOperation{TResult}.IsCompleted"/> becomes <see langword="true"/>).
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public new PlayerAccountOperation ContinueWith([DisallowNull] Action action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            base.ContinueWith(action, continuationOptions);
            return this;
        }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="PlayerAccountOperation{TResult}.IsCompleted"/> becomes <see langword="true"/>).
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public PlayerAccountOperation ContinueWith([DisallowNull] Action<PlayerAccountOperation> action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            if (IsCompleted)
            {
                if (!continuationOptions.HasFlag(TaskContinuationOptions.NotOnFaulted))
                {
                    MarkErrorAsObserved();
                }

                action(this);
                return this;
            }

            task.ContinueWith(_ =>
            {
                if (!continuationOptions.HasFlag(TaskContinuationOptions.NotOnFaulted))
                {
                    MarkErrorAsObserved();
                }

                action(this);
            }, cancellationToken, continuationOptions, TaskUtils.Scheduler);
            return this;
        }

        /// <inheritdoc cref="CloudOperation{TResult, PlayerAccountOperationError}.GetAwaiter()"/>
        public new TaskAwaiter<PlayerAccountOperation> GetAwaiter() => GetAwaiter(this);

        internal override PlayerAccountOperationError CreateError([DisallowNull] Exception exception, object args = null)
            => exception.TryExtract(out PlayerAccountOperationException playerAccountOperationException)
            ? new(playerAccountOperationException, errorHasBeenObserved)
            : new(PlayerAccountErrorType.InternalException, Coherence.Log.Error.RuntimeInternalException, exception.ToString(), errorHasBeenObserved);
    }
}
