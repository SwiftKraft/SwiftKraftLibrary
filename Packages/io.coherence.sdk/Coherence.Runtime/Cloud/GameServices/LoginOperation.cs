// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Common;
    using Runtime;
    using Utils;

    /// <summary>
    /// Represents an asynchronous operation attempting to login to <see cref="CoherenceCloud">coherence Cloud</see>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see langword="async"/> method can <see langword="await"/> the <see cref="LoginOperation"/> to wait until the operation has completed.
    /// </para>
    /// <para>
    /// Similarly, a <see cref="UnityEngine.Coroutine"/> can <see langword="yield"/> the <see cref="LoginOperation"/> to wait for it to complete.
    /// </para>
    /// <para>
    /// <see cref="ContinueWith(Action, TaskContinuationOptions)"/> can also be used to perform an action after the operation has completed.
    /// </para>
    /// <para>
    /// <see cref="OnSuccess(Action{PlayerAccount})"/> and <see cref="OnFail(Action{LoginOperationError})"/> can be used to perform different actions based on
    /// whether the operation was successful or not.
    /// </para>
    /// <para>
    /// If <see cref="LoginOperation.HasFailed"/> is <see langword="true"/>, then the operation has failed. If this is the case, then
    /// <see cref="LoginOperation.Error"/> will be non-<see langword="null"/> and contain additional information about the error.
    /// </para>
    /// <para>
    /// If a <see cref="LoginOperation"/> fails, and the error is not handled in any way (<see cref="LoginOperation.HasFailed"/> is not
    /// checked, <see cref="LoginOperation.Error"/> is not accessed, <see cref="OnFail"/> is not used, etc.), then the error will automatically
    /// be logged to the Console at some point (whenever the garbage collector releases the <see cref="LoginOperationError"/> from memory).
    /// </para>
    /// </remarks>
    public sealed class LoginOperation : CloudOperation<PlayerAccount, LoginOperationError>
    {
        /// <summary>
        /// Identifiers of all lobbies that the logged-in player account has joined.
        /// </summary>
        /// <remarks>
        /// If the operation has not completed successfully, this will be <see langword="null"/>.
        /// </remarks>>
        public IReadOnlyList<string> LobbyIds => IsCompletedSuccessfully ? task.Result.LoginResult.LobbyIds : null;

        /// <summary>
        /// State of the legacy key-value store associated with the logged-in player account.
        /// </summary>
        /// <remarks>
        /// If the operation has not completed successfully, this will be <see langword="null"/>.
        /// </remarks>>
        /// <seealso cref="CloudStorage"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IReadOnlyList<KeyValuePair<string, string>> KeyValuePairStoreState => IsCompletedSuccessfully ? task.Result.LoginResult.KeyValuePairStoreState : null;

        internal LoginOperation(Task<PlayerAccount> task) : base(task) { }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="LoginOperation.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public new LoginOperation ContinueWith([DisallowNull] Action action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
        {
            base.ContinueWith(action, continuationOptions);
            return this;
        }

        /// <summary>
        /// Specify an action to perform after the operation has completed
        /// (<see cref="LoginOperation.IsCompleted"/> becomes <see langword="true"/>.
        /// </summary>
        /// <param name="action"> Reference to a function to execute when the operation has completed. </param>
        /// <param name="continuationOptions"> Options for when the continuation should be scheduled and how it behaves. </param>
        public LoginOperation ContinueWith([DisallowNull] Action<LoginOperation> action, TaskContinuationOptions continuationOptions = TaskContinuationOptions.NotOnCanceled)
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
        /// (<see cref="LoginOperation.IsCompletedSuccessfully"/> becomes <see langword="true"/>).
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has completed successfully.
        /// </param>
        public new LoginOperation OnSuccess([DisallowNull] Action<PlayerAccount> action)
        {
            base.OnSuccess(action);
            return this;
        }

        /// <summary>
        /// Specify an action to perform if the operation fails (<see cref="LoginOperation.HasFailed"/> becomes
        /// <see langword="true"/>).
        /// </summary>
        /// <param name="action">
        /// Reference to a function to execute if and when the operation has failed.
        /// </param>
        public new LoginOperation OnFail([DisallowNull] Action<LoginOperationError> action)
        {
            base.OnFail(action);
            return this;
        }

        /// <inheritdoc cref="CloudOperation{PlayerAccount, LoginOperationError}.GetAwaiter()"/>
        public new TaskAwaiter<LoginOperation> GetAwaiter() => GetAwaiter(this);

        internal override LoginOperationError CreateError([DisallowNull] Exception exception, object args = null)
            => exception.TryExtract(out LoginError loginError) ? new(loginError.LoginErrorType, loginError.Message) : new(LoginErrorType.None, exception.ToString());
    }
}
