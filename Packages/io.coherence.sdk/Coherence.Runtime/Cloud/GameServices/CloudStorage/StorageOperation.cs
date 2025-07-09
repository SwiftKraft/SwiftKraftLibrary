// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Utils;
    using Runtime;

    /// <summary>
    /// Represents an asynchronous operation attempting to retrieve <see cref="StorageItem">items</see> from <see cref="CloudStorage"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see langword="async"/> method can <see langword="await"/> the <see cref="StorageOperation{TResult}"/> to wait until the operation has completed.
    /// </para>
    /// <para>
    /// Similarly, a <see cref="UnityEngine.Coroutine"/> can <see langword="yield"/> the <see cref="StorageOperation{TResult}"/> to wait for it to complete.
    /// </para>
    /// <para>
    /// <see cref="ContinueWith(Action, TaskContinuationOptions)"/> can also be used to perform an action after the operation has completed.
    /// </para>
    /// <para>
    /// <see cref="OnSuccess(Action{TResult})"/> and <see cref="OnFail(Action{StorageError})"/> can be used to perform different actions based on
    /// whether the operation was successful or not.
    /// </para>
    /// <para>
    /// If <see typeparamref="TResult"/> is a collection, you can enumerate the <see cref="StorageOperation{TResult}"/> using <see langword="foreach"/>
    /// to access each element in the collection.
    /// </para>
    /// <para>
    /// If <see cref="StorageOperation{TResult}.HasFailed"/> is <see langword="true"/>, then the operation has failed. If this is the case, then
    /// <see cref="CloudOperation{TError}.Error"/> will be non-<see langword="null"/> and contain additional information about the error.
    /// </para>
    /// <para>
    /// If a <see cref="StorageOperation{TResult}"/> fails, and the error is not handled in any way (<see cref="StorageOperation{TResult}.HasFailed"/> is not
    /// checked, <see cref="StorageOperation{TResult}.Error"/> is not accessed, <see cref="OnFail"/> is not used, etc.), then the error will automatically
    /// be logged to the Console at some point (whenever the garbage collector releases the <see cref="StorageError"/> from memory).
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult"> Type of object returned if the operation succeeds. </typeparam>
    /// <example>
    /// <code source="Runtime/CloudStorage/StorageOperationResultExample.cs" language="csharp"/>
    /// </example>
    public sealed class StorageOperation<TResult> : CloudOperation<TResult, StorageError>
    {
        internal StorageOperation(Task<TResult> task) : base(task) { }
        internal StorageOperation(StorageException exception) : base(Task.FromException<TResult>(exception)) { }
        internal StorageOperation(Task<TResult> task, CancellationToken cancellationToken) : base(task, cancellationToken) { }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="CloudOperation{TError}.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public new StorageOperation<TResult> ContinueWith([DisallowNull] Action action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            base.ContinueWith(action, continuationOptions);
            return this;
        }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="StorageOperation{TResult}.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public StorageOperation<TResult> ContinueWith([DisallowNull] Action<StorageOperation<TResult>> action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
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
        /// (<see cref="StorageOperation{TResult}.IsCompletedSuccessfully"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has completed successfully.
        /// </param>
        public new StorageOperation<TResult> OnSuccess([DisallowNull] Action<TResult> action)
        {
            base.OnSuccess(action);
            return this;
        }

        /// <summary>
        /// Specify an action to perform if the operation fails (<see cref="CloudOperation{TError}.HasFailed"/> becomes
        /// <see langword="true"/>.
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has failed.
        /// </param>
        public new StorageOperation<TResult> OnFail([DisallowNull] Action<StorageError> action)
        {
            base.OnFail(action);
            return this;
        }

        /// <inheritdoc cref="CloudOperation{TResult, StorageError}.GetAwaiter()"/>
        public new TaskAwaiter<StorageOperation<TResult>> GetAwaiter() => GetAwaiter(this);

        internal override StorageError CreateError(Exception exception, object storageObjectIds = null)
        {
            var ids = storageObjectIds as StorageObjectId[];
            if (exception.TryExtract(out StorageException storageException))
            {
                return new(storageException.ErrorType, storageException.ToString(), Log.Error.StorageOperationError, errorHasBeenObserved);
            }

            storageException = StorageException.From(exception, ids);
            return exception.TryExtract(out RequestException requestException)
                ? new(requestException, ids, Log.Error.StorageOperationError, errorHasBeenObserved)
                : new(storageException.ErrorType, storageException.ToString(), Log.Error.StorageOperationError, errorHasBeenObserved);
        }

        public static implicit operator StorageOperation<TResult>(Task<TResult> task) => new(task);
        public static implicit operator StorageOperation<TResult>(Exception exception) => new(Task.FromException<TResult>(exception));

        protected override string ResultToString([DisallowNull] TResult result) => result.GetType().Name;
    }

    /// <summary>
    /// Represents an asynchronous <see cref="CloudStorage"/> operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see langword="async"/> method can <see langword="await"/> the <see cref="StorageOperation"/> to wait until the operation has completed.
    /// </para>
    /// <para>
    /// Similarly, a <see cref="UnityEngine.Coroutine"/> can <see langword="yield"/> the <see cref="StorageOperation"/> to wait for it to complete.
    /// </para>
    /// <para>
    /// <see cref="ContinueWith(Action, TaskContinuationOptions)"/> can also be used to perform an action after the operation has completed.
    /// </para>
    /// <para>
    /// <see cref="OnSuccess(Action)"/> and <see cref="OnFail(Action{StorageError})"/> can be used to perform different actions based on
    /// whether the operation was successful or not.
    /// </para>
    /// <para>
    /// If a <see cref="StorageOperation"/> fails, and the error is not handled in any way (<see cref="StorageOperation.HasFailed"/> is not
    /// checked, <see cref="StorageOperation.Error"/> is not accessed, <see cref="OnFail"/> is not used, etc.), then the error will automatically
    /// be logged to the Console at some point (whenever the garbage collector releases the <see cref="StorageError"/> from memory).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code source="Runtime/CloudStorage/StorageOperationExample.cs" language="csharp"/>
    /// </example>
    public sealed class StorageOperation : CloudOperation<StorageError>
    {
        internal StorageOperation(Task task) : base(task) { }
        internal StorageOperation(Task task, CancellationToken cancellationToken) : base(task, cancellationToken) { }
        internal StorageOperation(StorageException exception) : base(Task.FromException(exception)) { }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="StorageOperation.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public new StorageOperation ContinueWith([DisallowNull] Action action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            if (IsCompleted)
            {
                if (!continuationOptions.HasFlag(TaskContinuationOptions.NotOnFaulted))
                {
                    error?.Ignore();
                }

                action();
                return this;
            }

            task.ContinueWith(_ =>
            {
                if (!continuationOptions.HasFlag(TaskContinuationOptions.NotOnFaulted))
                {
                    error?.Ignore();
                }

                action();
            }, default, continuationOptions, TaskUtils.Scheduler);

            return this;
        }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="StorageOperation.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public StorageOperation ContinueWith([DisallowNull] Action<StorageOperation> action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
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
        /// (<see cref="CloudOperation{TError}.IsCompletedSuccessfully"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has completed successfully.
        /// </param>
        public new StorageOperation OnSuccess([DisallowNull] Action action)
        {
            if (IsCompletedSuccessfully)
            {
                action();
                return this;
            }

            task.ContinueWith(completedTask =>
            {
                if (completedTask.IsCompletedSuccessfully)
                {
                    action();
                }
            }, default, TaskContinuationOptions.OnlyOnRanToCompletion, TaskUtils.Scheduler);
            return this;
        }

        /// <summary>
        /// Specify an action to perform if the operation fails (<see cref="CloudOperation{TError}.HasFailed"/> becomes
        /// <see langword="true"/>.
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has failed.
        /// </param>
        public new StorageOperation OnFail([DisallowNull] Action<StorageError> action)
        {
            base.OnFail(action);
            return this;
        }

        /// <inheritdoc cref="CloudOperation{StorageError}.GetAwaiter()"/>
        public new TaskAwaiter<StorageOperation> GetAwaiter() => GetAwaiter(this);

        internal override StorageError CreateError(Exception exception, object storageObjectIds = null)
        {
            var ids = storageObjectIds as StorageObjectId[];
            var storageException = StorageException.From(exception, ids);
            return exception.TryExtract(out RequestException requestException)
                ? new(requestException, ids, Log.Error.StorageOperationError, errorHasBeenObserved)
                : new(storageException.ErrorType, storageException.Message, Log.Error.StorageOperationError, errorHasBeenObserved);
        }

        public static implicit operator StorageOperation(Task task) => new(task);

        internal static StorageOperation<TResult> ToSingleResultOperation<TResult>(StorageOperation<TResult[]> multiResultOperation, StorageObjectId storageObjectId)
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>();
            var singleResultOperation = new StorageOperation<TResult>(taskCompletionSource.Task, multiResultOperation.cancellationToken);

            multiResultOperation.task.ContinueWith(task =>
            {
                if (!multiResultOperation.errorHasBeenObserved)
                {
                    // Mark the error in the multi-result operation as observed to prevent errors getting logged twice,
                    // or getting logged even if they have been observed via the single-result operation.
                    multiResultOperation.MarkErrorAsObserved();
                }
                else
                {
                    singleResultOperation.MarkErrorAsObserved();
                }

                try
                {
                    if (task.IsFaulted)
                    {
                        singleResultOperation.error = multiResultOperation.error;
                        taskCompletionSource.SetException(task.Exception);
                    }
                    else if (task.IsCanceled)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                    else if (task.Result.Length is 0)
                    {
                        var exception = StorageException.StorageObjectNotFound(storageObjectId);
                        var error = multiResultOperation.CreateError(exception, new[] { storageObjectId  });
                        multiResultOperation.error = error;
                        singleResultOperation.error = error;
                    }
                    else if (task.Result.Length > 1)
                    {
                        var exception = new InvalidOperationException($"{nameof(StorageOperation)}<{typeof(TResult).Name}[]> results array should only be converted to a single result when exactly one storage object is being requested.");
                        var error = multiResultOperation.CreateError(exception, new[] { storageObjectId } );
                        multiResultOperation.error = error;
                        singleResultOperation.error = error;
                        taskCompletionSource.SetException(exception);
                    }
                    else
                    {
                        taskCompletionSource.SetResult(task.Result[0]);
                    }
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            }, default, TaskContinuationOptions.ExecuteSynchronously, TaskUtils.Scheduler);

            return singleResultOperation;
        }
    }
}
