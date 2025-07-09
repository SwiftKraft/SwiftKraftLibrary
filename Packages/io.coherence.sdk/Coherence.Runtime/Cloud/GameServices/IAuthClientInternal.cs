// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Runtime;

    /// <summary>
    /// Exposes additional members on top of the ones already offered by <see cref="IAuthClient"/>
    /// for internal-only use.
    /// </summary>
    /// <inheritdoc/>
    internal interface IAuthClientInternal : IAuthClient
    {
        event Action<PlayerAccount> OnLoggingIn;
        event Action<PlayerAccount> OnLoggingOut;
        PlayerAccount PlayerAccount { get; set; }
        PlayerAccountId PlayerAccountId { get; }
        SessionToken SessionToken { get; }

        Task<LoginResult> Login(LoginInfo info, CancellationToken cancellationToken = default);

        PlayerAccountOperation PlayerAccountOperationAsync<TRequest>(PlayerAccountOperationInfo<TRequest> info, CancellationToken cancellationToken, Action onCompletingSuccessfully) where TRequest : struct, IPlayerAccountOperationRequest
        {
            var operation = PlayerAccountOperationAsync<TRequest, PlayerAccountOperationNullResponse, bool>(info, null, cancellationToken, onCompletingSuccessfully);
            var result = new PlayerAccountOperation(operation.task);
            if (operation.errorHasBeenObserved)
            {
                result.MarkErrorAsObserved();
            }
            else
            {
                operation.MarkErrorAsObserved();
            }

            return result;
        }

        PlayerAccountOperation<TResult> PlayerAccountOperationAsync<TRequest, TResponse, TResult>(PlayerAccountOperationInfo<TRequest> info, Func<TResponse, TResult> resultFactory, CancellationToken cancellationToken, Action onCompletingSuccessfully) where TRequest : struct, IPlayerAccountOperationRequest where TResponse : IPlayerAccountOperationResponse;
    }
}
