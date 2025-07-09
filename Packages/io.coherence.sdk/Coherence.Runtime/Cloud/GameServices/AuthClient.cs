// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Log;
    using Prefs;
    using Runtime;
    using Runtime.Utils;
    using Utils;
    using Logger = Log.Logger;

    /// <summary>
    /// Specifies a set of methods that can be used to log in to coherence Cloud, properties to determine
    /// if we are currently logged in or not, and events for getting notified about relevant things happening.
    /// </summary>
    public sealed class AuthClient : IAuthClientInternal, IDisposableInternal
    {
        internal const string LoginRequestName = nameof(AuthClient) + ".Login";
        internal const string UserOperationName = nameof(AuthClient) + ".PlayerAccountOperation";
        internal const string LoginRequestMethod = "POST";
        private const string ConnectionClosedPath = "/connection/closed";

        private Action<PlayerAccount> onLoggingIn;
        event Action<PlayerAccount> IAuthClientInternal.OnLoggingIn
        {
            add => onLoggingIn += value;
            remove => onLoggingIn -= value;
        }

        private Action<PlayerAccount> onLoggingOut;
        event Action<PlayerAccount> IAuthClientInternal.OnLoggingOut
        {
            add => onLoggingOut += value;
            remove => onLoggingOut -= value;
        }

        /// <inheritdoc/>
        public event Action<LoginResponse> OnLogin;

        /// <inheritdoc/>
        public event Action OnLogout;

        /// <inheritdoc/>
        public event Action<LoginError> OnError;

        internal event Action BeingDisposed;

        /// <inheritdoc/>
        public bool LoggedIn { get; private set; }

        PlayerAccount IAuthClientInternal.PlayerAccount
        {
            get => playerAccount;

            set
            {
                if (ReferenceEquals(playerAccount, value))
                {
                    return;
                }

                playerAccount?.Dispose();
                playerAccount = value;
            }
        }
        SessionToken IAuthClientInternal.SessionToken => playerAccount?.SessionToken ?? SessionToken.None;
        PlayerAccountId IAuthClientInternal.PlayerAccountId => playerAccount?.Id ?? PlayerAccountId.None;
        string IDisposableInternal.InitializationContext { get; set; }
        string IDisposableInternal.InitializationStackTrace { get; set; }
        bool IDisposableInternal.IsDisposed { get => isDisposed; set => isDisposed = value; }
        private readonly IRequestFactory requestFactory;
        private readonly TimeSpan simulatorTokenRefreshPeriodInDays = TimeSpan.FromDays(1f);
        private PlayerAccount playerAccount;
        private SessionToken SessionToken => playerAccount.SessionToken;
        private CloudUniqueId UniqueId => playerAccountProvider.CloudUniqueId;
        private string ProjectId => playerAccountProvider.ProjectId;
        private CancellationTokenSource initialAuthCancellationToken;
        private Task refreshTokenTask;
        private Action onWebSocketConnect;
        private bool isDisposed;
        private readonly IPlayerAccountProvider playerAccountProvider;
        private bool shouldDisposePlayerAccountProvider;
        private readonly Logger logger = Log.GetLogger<AuthClient>();

        /// <summary>
        /// Initializes a new instance of <see cref="AuthClient"/> for a player.
        /// </summary>
        public static AuthClient ForPlayer(IRequestFactory requestFactory, string projectId)
            => new(new NewPlayerAccountProvider(null, projectId), requestFactory) { shouldDisposePlayerAccountProvider = true };

        /// <inheritdoc cref="ForPlayer(IRequestFactory, string)"/>
        internal static AuthClient ForPlayer(IRequestFactory requestFactory, IPlayerAccountProvider playerAccountProvider)
            => new(playerAccountProvider, requestFactory);

#if UNITY
        /// <summary>
        /// Initializes a new instance of <see cref="AuthClient"/> for a simulator.
        /// </summary>
        internal static AuthClient ForSimulator(IRequestFactory requestFactory, SimulatorPlayerAccountProvider playerAccountProvider)
        {
            var authClient = new AuthClient(playerAccountProvider, requestFactory);

            if (string.IsNullOrEmpty(authClient.ProjectId))
            {
                return authClient;
            }

            if (string.IsNullOrEmpty(SimulatorUtility.AuthToken))
            {
                authClient.logger.Error(Error.RuntimeCloudSimulatorMissingToken,
                    $"{nameof(ForSimulator)} was used but {nameof(SimulatorUtility)}.{nameof(SimulatorUtility.AuthToken)} was null or empty.");
            }

            requestFactory.OnWebSocketConnect += authClient.InitializeSimulatorAuthentication;

            if (requestFactory.IsReady)
            {
                authClient.InitializeSimulatorAuthentication();
            }

            return authClient;
        }
#endif

        private AuthClient(IPlayerAccountProvider playerAccountProvider, IRequestFactory requestFactory)
        {
            this.OnInitialized();
            this.playerAccountProvider = playerAccountProvider;
            this.requestFactory = requestFactory;
        }

        /// <inheritdoc/>
        public async Task<LoginResult> LoginAsGuest(CancellationToken cancellationToken = default)
        {
            while (!playerAccountProvider.IsReady)
            {
                await Task.Yield();
            }

            return await Login(LoginInfo.ForGuest(playerAccountProvider, preferLegacyLoginData: true), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithPassword(string username, string password, bool autoSignup, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithPassword(username, password, autoSignup), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithSessionToken(SessionToken sessionToken, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithSessionToken(sessionToken), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithJwt(string token, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithJwt(token), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithOneTimeCode(string code, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithOneTimeCode(code), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithSteam(string ticket, string identity = null, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithSteam(ticket, identity), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithEpicGames(string token, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithEpicGames(token), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithPlayStation(string token, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithPlayStation(token), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithXbox(string token, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithXbox(token), cancellationToken);

        /// <inheritdoc/>
        public async Task<LoginResult> LoginWithNintendo(string token, CancellationToken cancellationToken = default) => await Login(LoginInfo.WithNintendo(token), cancellationToken);

        /// <inheritdoc/>
        public void Logout()
        {
            if (!LoggedIn)
            {
                return;
            }

            onLoggingOut?.Invoke(playerAccount);
            LoggedIn = false;
            OnLogout?.Invoke();
        }

        ~AuthClient()
        {
#if UNITY
            if (SimulatorUtility.UseSharedCloudCredentials)
            {
                logger.Info($"Won't call {nameof(Dispose)} even through finalizer was executed, because {nameof(SimulatorUtility)}.{nameof(SimulatorUtility.UseSharedCloudCredentials)} is True, and we want to preserve AuthClient instance's integrity for Simulators running in the Cloud.");
                return;
            }
#endif

            if (!this.OnFinalized())
            {
                logger.Warning(Warning.RuntimeCloudGameServicesResourceLeak,
                    this.GetResourceLeakWarningMessage() + $".\n{nameof(UniqueId)}:\"{UniqueId}\".");
            }
        }

        public void Dispose()
        {
            if (this.OnDisposed())
            {
                return;
            }

            GC.SuppressFinalize(this);

            BeingDisposed?.Invoke();
            BeingDisposed = null;

            try
            {
                Prefs.Save();
            }
            catch (Exception ex)
            {
                logger.Warning(Warning.PrefsFailedToSave, ("exception", ex));
            }

            if (onWebSocketConnect is not null)
            {
                requestFactory.OnWebSocketConnect -= onWebSocketConnect;
            }

            requestFactory.RemovePushCallback(ConnectionClosedPath, OnConnectionClosedHandler);

            initialAuthCancellationToken?.Cancel();
            initialAuthCancellationToken?.Dispose();

            logger?.Dispose();

            if (shouldDisposePlayerAccountProvider)
            {
                playerAccountProvider?.Dispose();
                shouldDisposePlayerAccountProvider = false;
            }

            LoggedIn = false;
        }

        private void OnConnectionClosedHandler(string responseBody)
        {
            try
            {
                Logout();
                HandleLoginError(default, Result.ServerError, LoginErrorType.ServerError, ErrorType.ServerError, Error.RuntimeServerError,  responseBody, logWarning: Warning.RuntimeCloudGameServicesShutdownError);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(ConnectionClosedResponse)),
                    ("Response", responseBody),
                    ("exception", exception));
            }
        }

        Task<LoginResult> IAuthClientInternal.Login(LoginInfo info, CancellationToken cancellationToken) => Login(info, cancellationToken);

        internal async Task<LoginResult> Login(LoginInfo info, CancellationToken cancellationToken = default)
        {
            if (PlayerAccount.Find(info) is { IsLoggedIn: true } existingUser)
            {
                LoggedIn = true;
                playerAccount = existingUser;
                return existingUser.LoginResult;
            }

            if (LoggedIn)
            {
                return HandleLoginError(info.LoginType, Result.AlreadyLoggedIn, LoginErrorType.AlreadyLoggedIn, ErrorType.AlreadyLoggedIn, Error.RuntimeAlreadyLoggedIn);
            }

            while (!playerAccountProvider.IsReady)
            {
                await Task.Yield();
            }

            try
            {
                playerAccount ??= playerAccountProvider.GetPlayerAccount(info);
            }
            catch (Exception ex)
            {
                return HandleLoginError(info.LoginType, Result.InvalidCredentials, LoginErrorType.InvalidCredentials, ErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials, exception: ex, logWarning: Warning.RuntimeInternalException);
            }

            if (playerAccount.IsLoggedIn)
            {
                LoggedIn = true;
                return playerAccount.LoginResult;
            }

            if (!string.IsNullOrEmpty(playerAccountProvider.ProjectId))
            {
                requestFactory.RemovePushCallback(ConnectionClosedPath, OnConnectionClosedHandler);
                requestFactory.AddPushCallback(ConnectionClosedPath, OnConnectionClosedHandler);
            }

            if (info.LoginType is LoginType.Guest)
            {
                logger.Debug($"Logging in as guest", ("guestId", info.GuestId),
                    ("projectId", ProjectId), ("uniqueId", UniqueId));

                GuestId.Save(ProjectId, UniqueId, info.GuestId);
            }

            playerAccount.AddLoginInfo(info);

            #if UNITY && UNITY_2022_2_OR_NEWER
            if (cancellationToken == CancellationToken.None)
            {
                cancellationToken = UnityEngine.Application.exitCancellationToken;
            }
            #endif

            onLoggingIn?.Invoke(playerAccount);

            var connectionResult = await WaitUntilConnected();

            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<LoginResult>(cancellationToken);
            }

            if (!connectionResult.success)
            {
                return HandleLoginError(info.LoginType, Result.ConnectionError, LoginErrorType.ConnectionError, ErrorType.ConnectionError, Error.RuntimeConnectionError, exception:new LoginError(ErrorType.ServerError, LoginErrorType.ServerError, Error.RuntimeServerError, connectionResult.error), logWarning:Warning.RuntimeConnectionError);
            }

            initialAuthCancellationToken?.Cancel();

            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<LoginResult>(cancellationToken);
            }

            var result = await HandleLoginRequestAsync(info);

            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<LoginResult>(cancellationToken);
            }

            return result;

            async Task<LoginResult> HandleLoginRequestAsync(LoginInfo info)
            {
                var body = GetRequestBody(info);
                var basePath = info.LoginType.GetBasePath();
                LoginResult loginResponse;

                try
                {
                    var response = await requestFactory.SendRequestAsync(basePath, LoginRequestMethod, body, null, LoginRequestName, SessionToken);
                    loginResponse = HandleLoginResponse(info.LoginType, response);
                }
                catch (RequestException ex)
                {
                    var(resultType, loginErrorType, errorType, error) = ex.ErrorCode switch
                    {
                        ErrorCode.InvalidCredentials or ErrorCode.LoginInvalidUsername or ErrorCode.LoginInvalidPassword => (Result.InvalidCredentials, LoginErrorType.InvalidCredentials, ErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials),
                        ErrorCode.TooManyRequests => (Result.TooManyRequests, LoginErrorType.TooManyRequests, ErrorType.TooManyRequests, Error.RuntimeTooManyRequests),
                        ErrorCode.FeatureDisabled or ErrorCode.LoginDisabled => (Result.FeatureDisabled, LoginErrorType.InvalidConfig, ErrorType.FeatureDisabled, Error.RuntimeFeatureDisabled),
                        ErrorCode.InvalidConfig => (Result.InvalidConfig, LoginErrorType.InvalidConfig, ErrorType.InvalidConfig, Error.RuntimeInvalidConfig),
                        ErrorCode.LoginInvalidApp => (Result.InvalidApp, LoginErrorType.ServerError, ErrorType.InvalidApp, Error.RuntimeInvalidApp),
                        ErrorCode.OneTimeCodeExpired => (Result.OneTimeCodeExpired, LoginErrorType.OneTimeCodeExpired, ErrorType.OneTimeCodeExpired, Error.RuntimeOneTimeCodeExpired),
                        ErrorCode.LoginNotFound => (Result.OneTimeCodeNotFound, LoginErrorType.OneTimeCodeNotFound, ErrorType.OneTimeCodeNotFound, Error.RuntimeIdentityNotFound),
#pragma warning disable CS0618
                        ErrorCode.LoginWeakPassword => (Result.InvalidCredentials, LoginErrorType.InvalidCredentials, ErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials),
#pragma warning restore CS0618
                        ErrorCode.IdentityLimit => (Result.IdentityLimit, LoginErrorType.IdentityLimit, ErrorType.IdentityLimit, Error.RuntimeIdentityLimit),
                        ErrorCode.IdentityNotFound => (Result.IdentityNotFound, LoginErrorType.IdentityNotFound, ErrorType.IdentityNotFound, Error.RuntimeIdentityNotFound),
                        ErrorCode.IdentityTaken => (Result.IdentityTaken, LoginErrorType.IdentityTaken, ErrorType.IdentityTaken, Error.RuntimeIdentityTaken),
                        ErrorCode.IdentityTotalLimit => (Result.IdentityTotalLimit, LoginErrorType.IdentityTotalLimit, ErrorType.IdentityTotalLimit, Error.RuntimeIdentityTotalLimit),
                        ErrorCode.InvalidInput => (Result.InvalidInput, LoginErrorType.InvalidInput, ErrorType.InvalidInput, Error.RuntimeInvalidInput),
                        ErrorCode.PasswordNotSet => (Result.PasswordNotSet, LoginErrorType.PasswordNotSet, ErrorType.PasswordNotSet, Error.RuntimePasswordNotSet),
                        ErrorCode.UsernameNotAvailable => (Result.UsernameNotAvailable, LoginErrorType.UsernameNotAvailable, ErrorType.UsernameNotAvailable, Error.RuntimeUsernameNotAvailable),
                        _ => (Result.ServerError, LoginErrorType.ServerError, ErrorType.ServerError, Error.RuntimeServerError)
                    };

                    return HandleLoginError(info.LoginType, resultType, loginErrorType, errorType, error, "", ex, Warning.RuntimeCloudLoginFailedMsg);
                }

                // When legacy guest login data exists, we need to do the following:
                // 1. Log in using the legacy login data.
                // 2. Generate an save a new GuestId for the player account.
                // 3. Link the new GuestId to the player account.
                // 4. Delete the legacy login data.
                if (loginResponse.Type is Result.Success && info.LoginType is LoginType.LegacyGuest)
                {
                    var newLoginData = LoginInfo.ForGuest(playerAccountProvider, preferLegacyLoginData: false);
                    var linkOperation = await playerAccount.LinkGuest(newLoginData.GuestId, force: true);
                    if (linkOperation.IsCompletedSuccessfully)
                    {
                        LegacyLoginData.Clear(ProjectId, UniqueId);
                    }
                    else if (linkOperation.HasFailed)
                    {
                        Logout();
                        var error = linkOperation.Error;
                        var errorMessage = $"Failed to migrate legacy guest account to use new guest id.\nError: {error}";
                        logger.Warning(Warning.RuntimeCloudLoginFailedMsg, errorMessage);
                        loginResponse = linkOperation.Error.Type switch
                        {
                            PlayerAccountErrorType.ServerError => HandleLoginError(info.LoginType, Result.ServerError, LoginErrorType.ServerError, ErrorType.ServerError, Error.RuntimeServerError, errorMessage),
                            PlayerAccountErrorType.InvalidConfig => HandleLoginError(info.LoginType, Result.InvalidConfig, LoginErrorType.InvalidConfig, ErrorType.InvalidConfig, Error.RuntimeInvalidConfig, errorMessage),
                            PlayerAccountErrorType.InvalidApp => HandleLoginError(info.LoginType, Result.InvalidApp, LoginErrorType.ServerError, ErrorType.InvalidApp, Error.RuntimeInvalidApp, errorMessage),
                            PlayerAccountErrorType.FeatureDisabled => HandleLoginError(info.LoginType, Result.FeatureDisabled, LoginErrorType.InvalidConfig, ErrorType.FeatureDisabled, Error.RuntimeFeatureDisabled, errorMessage),
                            PlayerAccountErrorType.InvalidCredentials => HandleLoginError(info.LoginType, Result.InvalidCredentials, LoginErrorType.InvalidCredentials, ErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials, errorMessage),
                            PlayerAccountErrorType.InvalidResponse => HandleLoginError(info.LoginType, Result.InvalidResponse, LoginErrorType.InvalidResponse, ErrorType.InvalidResponse, Error.RuntimeInvalidResponse, errorMessage),
                            PlayerAccountErrorType.ConnectionError => HandleLoginError(info.LoginType, Result.ConnectionError, LoginErrorType.ConnectionError, ErrorType.ConnectionError, Error.RuntimeConnectionError, errorMessage),
                            PlayerAccountErrorType.TooManyRequests => HandleLoginError(info.LoginType, Result.TooManyRequests, LoginErrorType.TooManyRequests, ErrorType.TooManyRequests, Error.RuntimeTooManyRequests, errorMessage),
                            PlayerAccountErrorType.OneTimeCodeExpired => HandleLoginError(info.LoginType, Result.OneTimeCodeExpired, LoginErrorType.OneTimeCodeExpired, ErrorType.OneTimeCodeExpired, Error.RuntimeOneTimeCodeExpired, errorMessage),
                            PlayerAccountErrorType.OneTimeCodeNotFound => HandleLoginError(info.LoginType, Result.OneTimeCodeNotFound, LoginErrorType.OneTimeCodeNotFound, ErrorType.OneTimeCodeNotFound, Error.RuntimeIdentityNotFound, errorMessage),
                            PlayerAccountErrorType.IdentityLimit => HandleLoginError(info.LoginType, Result.IdentityLimit, LoginErrorType.IdentityLimit, ErrorType.IdentityLimit, Error.RuntimeIdentityLimit, errorMessage),
                            PlayerAccountErrorType.IdentityNotFound => HandleLoginError(info.LoginType, Result.IdentityNotFound, LoginErrorType.IdentityNotFound, ErrorType.IdentityNotFound, Error.RuntimeIdentityNotFound, errorMessage),
                            PlayerAccountErrorType.IdentityTaken => HandleLoginError(info.LoginType, Result.IdentityTaken, LoginErrorType.IdentityTaken, ErrorType.IdentityTaken, Error.RuntimeIdentityTaken, errorMessage),
                            PlayerAccountErrorType.IdentityTotalLimit => HandleLoginError(info.LoginType, Result.IdentityTotalLimit, LoginErrorType.IdentityTotalLimit, ErrorType.IdentityTotalLimit, Error.RuntimeIdentityTotalLimit, errorMessage),
                            PlayerAccountErrorType.InvalidInput => HandleLoginError(info.LoginType, Result.InvalidInput, LoginErrorType.InvalidInput, ErrorType.InvalidInput, Error.RuntimeInvalidInput, errorMessage),
                            PlayerAccountErrorType.PasswordNotSet => HandleLoginError(info.LoginType, Result.PasswordNotSet, LoginErrorType.PasswordNotSet, ErrorType.PasswordNotSet, Error.RuntimePasswordNotSet, errorMessage),
                            PlayerAccountErrorType.UsernameNotAvailable => HandleLoginError(info.LoginType, Result.UsernameNotAvailable, LoginErrorType.UsernameNotAvailable, ErrorType.UsernameNotAvailable, Error.RuntimeUsernameNotAvailable, errorMessage),
                            _ => HandleLoginError(info.LoginType, Result.InvalidCredentials, LoginErrorType.InvalidCredentials, ErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials, errorMessage)
                        };
                    }
                }

                return loginResponse;
            }
        }

        PlayerAccountOperation<TResult> IAuthClientInternal.PlayerAccountOperationAsync<TRequest, TResponse, TResult>(PlayerAccountOperationInfo<TRequest> info, [MaybeNull] Func<TResponse, TResult> resultFactory, CancellationToken cancellationToken, Action onCompletingSuccessfully)
        {
            if (isDisposed)
            {
                return new(PlayerAccountErrorType.InternalException, Error.RuntimeInternalException, "Player account operation failed because cloud service object has already been disposed.");
            }

            if (!LoggedIn)
            {
                return new(PlayerAccountErrorType.NotLoggedIn, Error.RuntimeNotLoggedIn, "Player account operation failed because you are not logged in to coherence Cloud.");
            }

            var taskCompletionSource = new TaskCompletionSource<TResult>();
            WaitUntilConnected().Then(WhenConnected);
            var result = new PlayerAccountOperation<TResult>(taskCompletionSource.Task);
            return result;

            void WhenConnected(Task<(bool success, string error)> task)
            {
                if (cancellationToken.IsCancellationRequested || task.IsCanceled)
                {
                    taskCompletionSource.SetCanceled();
                    return;
                }

                if (task.IsFaulted)
                {
                    logger.Warning(Warning.RuntimeInternalException, task.Exception.Message);
                    taskCompletionSource.SetException(task.Exception);
                    return;
                }

                if (!task.Result.success)
                {
                    logger.Warning(Warning.RuntimeConnectionError, task.Result.error);
                    taskCompletionSource.SetException(new PlayerAccountOperationException(PlayerAccountErrorType.ConnectionError, Error.RuntimeConnectionError, task.Result.error));
                    return;
                }

                var basePath = info.BasePath;
                var pathParams = info.PathParams;
                var method = info.Method;
                var body = info.Request is { } request ? CoherenceJson.SerializeObject(request) : "";
                requestFactory.SendRequestAsync(basePath, pathParams, method, body, headers: null, UserOperationName, SessionToken).Then(OnRequestCompleted);
            }

            void OnRequestCompleted(Task<string> task)
            {
                if (cancellationToken.IsCancellationRequested || task.IsCanceled)
                {
                    taskCompletionSource.SetCanceled();
                    return;
                }

                if (task.IsFaulted)
                {
                    var errorCode = task.Exception.TryExtract(out RequestException requestException) ? requestException.ErrorCode : ErrorCode.Unknown;
                    var(errorType, error) = errorCode switch
                    {
                        ErrorCode.InvalidCredentials or ErrorCode.LoginInvalidUsername or ErrorCode.LoginInvalidPassword => (PlayerAccountErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials),
                        ErrorCode.TooManyRequests => (PlayerAccountErrorType.TooManyRequests, Error.RuntimeTooManyRequests),
                        ErrorCode.FeatureDisabled or ErrorCode.LoginDisabled => (PlayerAccountErrorType.FeatureDisabled, Error.RuntimeFeatureDisabled),
                        ErrorCode.InvalidConfig => (PlayerAccountErrorType.InvalidConfig, Error.RuntimeInvalidConfig),
                        ErrorCode.LoginInvalidApp => (PlayerAccountErrorType.InvalidApp, Error.RuntimeInvalidApp),
                        ErrorCode.OneTimeCodeExpired => (PlayerAccountErrorType.OneTimeCodeExpired, Error.RuntimeOneTimeCodeExpired),
                        ErrorCode.LoginNotFound => (PlayerAccountErrorType.OneTimeCodeNotFound, Error.RuntimeIdentityNotFound),
#pragma warning disable CS0618
                        ErrorCode.LoginWeakPassword => (PlayerAccountErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials),
#pragma warning restore CS0618
                        ErrorCode.IdentityLimit => (PlayerAccountErrorType.IdentityLimit, Error.RuntimeIdentityLimit),
                        ErrorCode.IdentityNotFound => (PlayerAccountErrorType.IdentityNotFound, Error.RuntimeIdentityNotFound),
                        ErrorCode.IdentityRemoval => (PlayerAccountErrorType.IdentityRemoval, Error.RuntimeIdentityRemoval),
                        ErrorCode.IdentityTaken => (PlayerAccountErrorType.IdentityTaken, Error.RuntimeIdentityTaken),
                        ErrorCode.IdentityTotalLimit => (PlayerAccountErrorType.IdentityTotalLimit, Error.RuntimeIdentityTotalLimit),
                        ErrorCode.InvalidInput => (PlayerAccountErrorType.InvalidInput, Error.RuntimeInvalidInput),
                        ErrorCode.PasswordNotSet => (PlayerAccountErrorType.PasswordNotSet, Error.RuntimePasswordNotSet),
                        ErrorCode.UsernameNotAvailable => (PlayerAccountErrorType.UsernameNotAvailable, Error.RuntimeUsernameNotAvailable),
                        _ => (PlayerAccountErrorType.ServerError, Error.RuntimeServerError)
                    };

                    taskCompletionSource.SetException(CreateUserOperationException(errorType, error, "", task.Exception));
                    return;
                }

                if (resultFactory is null)
                {
                    onCompletingSuccessfully?.Invoke();
                    taskCompletionSource.SetResult(default);
                    return;
                }

                const string InvalidResponseMessage = "Player account operation failed because was unable to deserialize the response from the server.\n\nResponse: \"{0}\"";
                var responseJson = task.Result;
                if (string.IsNullOrEmpty(responseJson))
                {
                    var errorMessage = string.Format(InvalidResponseMessage, "");
                    logger.Warning(Warning.RuntimeCloudDeserializationException, string.Format(errorMessage));
                    taskCompletionSource.SetException(new PlayerAccountOperationException(PlayerAccountErrorType.InvalidResponse, Error.RuntimeInvalidResponse, errorMessage));
                    return;
                }

                TResponse response;
                try
                {
                    response = CoherenceJson.DeserializeObject<TResponse>(responseJson);
                }
                catch (Exception exception)
                {
                    var errorMessage = string.Format(InvalidResponseMessage, responseJson) + "\n\nException: " + exception;
                    logger.Warning(Warning.RuntimeCloudDeserializationException, errorMessage);
                    taskCompletionSource.SetException(new PlayerAccountOperationException(PlayerAccountErrorType.InvalidResponse, Error.RuntimeInvalidResponse, errorMessage));
                    return;
                }

                try
                {
                    onCompletingSuccessfully?.Invoke();
                    taskCompletionSource.SetResult(resultFactory(response));
                }
                catch (Exception exception)
                {
                    const string ConversionFailedMessage = "Player account operation failed because was unable to generate result object based on response.\n\nResponse: \"{0}\"\n\nException: {1}";
                    var errorMessage = string.Format(ConversionFailedMessage, responseJson, exception);
                    logger.Warning(Warning.RuntimeCloudDeserializationException, errorMessage);
                    taskCompletionSource.SetException(new PlayerAccountOperationException(PlayerAccountErrorType.InvalidResponse, Error.RuntimeInvalidResponse, errorMessage));
                }
            }
        }

        private static PlayerAccountOperationException CreateUserOperationException(PlayerAccountErrorType errorType, Error error, string response = "", Exception exception = null)
        {
            return new(errorType, error,
#pragma warning disable CS8524
            errorType switch
#pragma warning restore CS8524
            {
                PlayerAccountErrorType.ServerError => "Player account operation failed because of server error.",
                PlayerAccountErrorType.InvalidCredentials => "Player account operation failed because invalid credentials were provided.",
                PlayerAccountErrorType.FeatureDisabled => "Player account operation failed because 'Persisted Player Accounts' is not enabled in the coherence Cloud > Dashboard > Project Settings.\n\nLink to Dashboard: https://coherence.io/dashboard/",
                PlayerAccountErrorType.InvalidResponse => "Player account operation failed because was unable to deserialize the response from the server.",
                PlayerAccountErrorType.TooManyRequests => "Player account operation failed because too many requests have been sent within a short amount of time.\n\nPlease slow down the rate of sending requests, and try again later.",
                PlayerAccountErrorType.ConnectionError => "Player account operation failed because of server error.",
                PlayerAccountErrorType.ConcurrentConnection
                    => "We have received a concurrent connection for your Player Account. Your current credentials will be invalidated.\n\n" +
                    "Usually this happens when a concurrent connection is detected, e.g. running multiple game clients for the same player.\n\n" +
                    "When this happens the game should present a prompt to the player to inform them that there is another instance of the game running. " +
                    "The game should wait for player input and never try to reconnect on its own or else the two game clients would disconnect each other indefinitely.",
                PlayerAccountErrorType.InvalidConfig
                    => "Player account operation failed because of invalid configuration in Online Dashboard." +
                       "\nMake sure that the feature has been enabled and all required configuration has been provided in Project Settings." +
                       "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                PlayerAccountErrorType.InvalidApp => "Player account operation failed because the provided App ID was invalid. Please check the App ID in the Project Settings of your Online Dashboard." +
                                                     "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                PlayerAccountErrorType.OneTimeCodeExpired => "Player account operation failed because the provided one-time code has expired. Please generate a new one-time ticket.",
                PlayerAccountErrorType.OneTimeCodeNotFound => "Player account operation failed because the provided one-time code was not found. Did you type it correctly?",
                PlayerAccountErrorType.IdentityLimit => "Player account operation failed because identity limit has been reached.",
                PlayerAccountErrorType.IdentityNotFound => "Player account operation failed because provided identity not found.",
                PlayerAccountErrorType.IdentityRemoval => "Player account operation failed because the the identity is already linked to another account, and it is the only authentication method that the other account has.\nPass a 'force' value of 'true' if you still want to unlink the identity from the other account. Note that this will result in access being lost to the other account.",
                PlayerAccountErrorType.IdentityTaken => "Player account operation failed because the identity is already linked to another account. Pass a 'force' value of 'true' to automatically unlink the authentication method from the other player account.",
                PlayerAccountErrorType.IdentityTotalLimit => "Player account operation failed because maximum allowed number of identities has been reached.",
                PlayerAccountErrorType.InvalidInput => "Player account operation failed due to invalid input.",
                PlayerAccountErrorType.PasswordNotSet => "Player account operation failed because password has not been set for the player account.",
                PlayerAccountErrorType.UsernameNotAvailable => "Player account operation failed because provided username is already taken by another player account.",
                PlayerAccountErrorType.InternalException => "Player account operation failed due to internal exception.",
                PlayerAccountErrorType.NotLoggedIn => "Player account operation failed because you are not logged in to coherence Cloud.",
            }
            + AppendUserMessage(exception)
            + AppendResponse(response));

            static string AppendUserMessage(Exception exception) => exception switch
            {
                null => "",
                _ when exception is RequestException { UserMessage: { Length: > 0 } message } => "\n\n" + message,
                _ => "\n\n" + exception,
            };

            static string AppendResponse(string response) => (response is { Length: > 0 } ? "\n\nResponse: " + response : "");
        }

        private async Task<(bool success, string error)> WaitUntilConnected()
        {
            if (requestFactory.IsReady)
            {
                return (true, null);
            }

            var webSocketConnectCompletionSource = new TaskCompletionSource<(bool success, string error)>();

            requestFactory.OnWebSocketConnect += OnWebSocketConnect;
            requestFactory.OnWebSocketDisconnect += OnWebSocketDisconnect;
            requestFactory.OnWebSocketConnectionError += OnWebSocketConnectionError;

            // Make sure that request factory starts connecting, if it hasn't already,
            // so that the OnWebSocketConnect event will get raised at some point.
            requestFactory.ForceCreateWebSocket();

            return await webSocketConnectCompletionSource.Task;

            void OnWebSocketConnect() => SetResult((true, null));
            void OnWebSocketDisconnect() => SetResult((false, "Logging in failed because connection was lost."));
            void OnWebSocketConnectionError() => SetResult((false, "Logging in failed because of server error."));

            void SetResult((bool success, string error) result)
            {
                requestFactory.OnWebSocketConnect -= OnWebSocketConnect;
                requestFactory.OnWebSocketDisconnect -= OnWebSocketDisconnect;
                requestFactory.OnWebSocketConnectionError -= OnWebSocketConnectionError;
                webSocketConnectCompletionSource.SetResult(result);
            }
        }

        private LoginResult HandleLoginError(LoginType loginType, Result resultType, LoginErrorType loginErrorType, ErrorType errorType, Error error, string response = "", Exception exception = null, Warning? logWarning = null)
        {
            var loginError = CreateLoginError(loginType, errorType, loginErrorType, error, response, exception);
            return HandleLoginError(loginError, resultType, logWarning);

            static LoginError CreateLoginError(LoginType loginType, ErrorType type, LoginErrorType loginErrorType, Error error, string response = "", Exception exception = null)
            {
                return new(type, loginErrorType, error,
#pragma warning disable CS8524
                type switch
#pragma warning restore CS8524
                {
                    ErrorType.ServerError => "Logging in failed because of server error.",
                    ErrorType.InvalidCredentials when loginType is LoginType.Password => $"Logging in failed because invalid username and password were provided.\nIf you are trying to log in to an existing account, make sure that the username and password were typed correctly.\nIf you are trying to create a new player account, pass an 'autoSignup' value of 'true' to {nameof(PlayerAccount)}.{nameof(CoherenceCloud.LoginWithPassword)}.",
                    ErrorType.InvalidCredentials => "Logging in failed because invalid credentials were provided.",
                    ErrorType.FeatureDisabled => "Logging in failed because 'Persisted Player Accounts' is not enabled in the coherence Cloud > Dashboard > Project Settings.\n\nLink to Dashboard: https://coherence.io/dashboard/",
                    ErrorType.InvalidResponse => "Logging in failed because was unable to deserialize the response from the server.",
                    ErrorType.TooManyRequests => "Logging in failed because too many requests have been sent within a short amount of time.\n\nPlease slow down the rate of sending requests, and try again later.",
                    ErrorType.ConnectionError => "Logging in failed because of server error.",
                    ErrorType.AlreadyLoggedIn => $"The cloud services are already connected to a player account. You have to call {nameof(GameServices)}.{nameof(GameServices.AuthService)}.{nameof(Logout)} before attempting to log in again.",
                    ErrorType.ConcurrentConnection
                        => "We have received a concurrent connection for your Player Account. Your current credentials will be invalidated.\n\n" +
                        "Usually this happens when a concurrent connection is detected, e.g. running multiple game clients for the same player.\n\n" +
                        "When this happens the game should present a prompt to the player to inform them that there is another instance of the game running. " +
                        "The game should wait for player input and never try to reconnect on its own or else the two game clients would disconnect each other indefinitely.",
                    ErrorType.InvalidConfig when loginType is LoginType.Steam => "Logging in with Steam failed because of invalid Online Dashboard configuration." +
                                                                                 "\nMake sure that 'Steam Auth Enabled' is ticked and that valid Application ID and Web API publisher authentication keys have been provided in Project Settings." +
                                                                                 "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.InvalidConfig when loginType is LoginType.EpicGames => "Logging in with Epic Games failed because of invalid Online Dashboard configuration." +
                                                                                 "\nMake sure that 'Epic Auth Enabled' is ticked and that valid Application ID has been provided in Project Settings." +
                                                                                 "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.InvalidConfig when loginType is LoginType.PlayStation => "Logging in with PlayStation Network account failed because of invalid Online Dashboard configuration." +
                                                                                    "\nMake sure that 'PSN Auth Enabled' is ticked in Project Settings." +
                                                                                    "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.InvalidConfig when loginType is LoginType.Xbox => "Logging in with Xbox profile failed because of invalid Online Dashboard configuration." +
                                                                                       "\nMake sure that 'Xbox Auth Enabled' is ticked in Project Settings." +
                                                                                       "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.InvalidConfig when loginType is LoginType.Nintendo => "Logging in with Nintendo account failed because of invalid Online Dashboard configuration." +
                                                                                     "\nMake sure that 'Nintendo Auth Enabled' is ticked in Project Settings." +
                                                                                     "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.InvalidConfig when loginType is LoginType.Jwt => "Logging in with JSON Web Token (JWT) failed because of invalid Online Dashboard configuration." +
                                                                                    "\nMake sure that 'JWT Auth Enabled' is ticked and that a valid JKU Domain or a Public Key has been provided." +
                                                                                    "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.InvalidConfig => "Logging in failed because of invalid configuration in Online Dashboard." +
                                               "\nMake sure that the authentication method has been enabled and all required configuration has been provided in Project Settings." +
                                               "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.InvalidApp => "Logging in failed because the provided App ID was invalid. Please check the App ID in the Project Settings of your Online Dashboard." +
                                            "\nOnline Dashboard can found be found at: https://coherence.io/dashboard",
                    ErrorType.OneTimeCodeExpired => "Logging in failed because the provided ticket has expired.",
                    ErrorType.OneTimeCodeNotFound => "Logging in failed because no account has been linked to the authentication method in question. Pass an 'autoSignup' value of 'true' to automatically create a new account if one does not exist yet.",
                    ErrorType.IdentityLimit => "Logging in failed because identity limit has been reached.",
                    ErrorType.IdentityNotFound => "Logging in failed because provided identity not found",
                    ErrorType.IdentityTaken => "Logging in failed because the identity is already linked to another account. Pass a 'force' value of 'true' to automatically unlink the authentication method from the other player account.",
                    ErrorType.IdentityTotalLimit => "Logging in failed because maximum allowed number of identities has been reached.",
                    ErrorType.InvalidInput when loginType is LoginType.Password => "Logging in failed due to invalid username or password being provided.",
                    ErrorType.InvalidInput => "Logging in failed due to invalid input.",
                    ErrorType.PasswordNotSet => "Logging in failed because password has not been set for the player account.",
                    ErrorType.UsernameNotAvailable => "Logging in failed because the provided username is already taken by another player account.",
                }
                + AppendUserMessage(exception)
                + AppendResponse(response),

                response);

                static string AppendUserMessage(Exception exception) => exception switch
                {
                    null => "",
                    _ when exception is RequestException { UserMessage: { Length: > 0 } message } => "\n\n" + message,
                    _ => "\n\n" + exception,
                };

                static string AppendResponse(string response) => (response is { Length: > 0 } ? "\n\nResponse: " + response : "");
            }
        }

        private LoginResult HandleLoginError(LoginError error, Result resultType, Warning? logWarning = null)
        {
            playerAccount.LoginResult = LoginResult.Failure(resultType, error);

            if (logWarning is not null)
            {
                logger.Warning(logWarning.Value, error.Message);
            }

            OnError?.Invoke(error);
            return playerAccount.LoginResult;
        }

        private static string GetRequestBody(LoginInfo loginInfo)
            => GetLoginRequest(loginInfo) is { } request ? CoherenceJson.SerializeObject(request) : "";

        [return: MaybeNull]
        #pragma warning disable CS8524
        private static object GetLoginRequest(LoginInfo info) => info.LoginType switch
        #pragma warning restore CS8524
        {
            LoginType.Steam => new SteamLoginRequest
            {
                Identity = info.Identity,
                Ticket = info.Ticket,
                AutoSignup = info.AutoSignup
            },
            LoginType.EpicGames => new EpicGamesLoginRequest
            {
                Token = info.Token,
                AutoSignup = info.AutoSignup
            },
            LoginType.PlayStation => new PlayStationLoginRequest
            {
                Token = info.Token,
                AutoSignup = info.AutoSignup
            },
            LoginType.Xbox => new XboxLoginRequest
            {
                Token = info.Token,
                AutoSignup = info.AutoSignup
            },
            LoginType.Nintendo => new NintendoLoginRequest
            {
                Token = info.Token,
                AutoSignup = info.AutoSignup
            },
            LoginType.Password => new PasswordLoginRequest
            {
                Username = info.Username,
                Password = info.Password,
                Autosignup = info.AutoSignup
            },
            LoginType.Guest => new GuestLoginRequest
            {
                GuestId = info.GuestId,
                AutoSignup = info.AutoSignup
            },
            LoginType.LegacyGuest => new PasswordLoginRequest
            {
                Username = info.Username,
                Password = info.Password,
                Autosignup = info.AutoSignup
            },
            LoginType.SessionToken => null,
            LoginType.OneTimeCode => new OneTimeCodeLoginRequest
            {
                Code = info.OneTimeCode
            },
            LoginType.Jwt => new JwtLoginRequest
            {
                Token = info.Token,
                AutoSignup = info.AutoSignup
            },
        };

        private LoginResult HandleLoginResponse(LoginType loginType, string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return HandleLoginError(loginType, Result.InvalidResponse, LoginErrorType.InvalidResponse, ErrorType.InvalidResponse, Error.RuntimeInvalidResponse, response, null, Warning.RuntimeCloudLoginFailedMsg);
            }

            LoginResponse loginResponse;
            try
            {
                loginResponse = CoherenceJson.DeserializeObject<LoginResponse>(response);
            }
            catch (Exception exception)
            {
                return HandleLoginError(loginType, Result.InvalidResponse, LoginErrorType.InvalidResponse, ErrorType.InvalidResponse, Error.RuntimeInvalidResponse, response, exception, Warning.RuntimeCloudLoginFailedMsg);
            }

            LoggedIn = true;
            playerAccount.LoginResult = LoginResult.Success(playerAccount, Result.Success, loginResponse);
            OnLogin?.Invoke(loginResponse);
            return playerAccount.LoginResult;
        }

#if UNITY
        private async void InitializeSimulatorAuthentication()
        {
            initialAuthCancellationToken = new CancellationTokenSource();

            if (string.IsNullOrEmpty(SessionToken))
            {
                logger.Error(Error.RuntimeCloudSimulatorAuthToken,
                    $"{nameof(InitializeSimulatorAuthentication)} called but {nameof(SessionToken)} was null or empty.");
                return;
            }

            refreshTokenTask ??= RefreshSessionTokenPeriodically();
            await refreshTokenTask;

            async Task RefreshSessionTokenPeriodically()
            {
                while (!initialAuthCancellationToken.IsCancellationRequested)
                {
                    await RefreshSessionToken();
                    await simulatorTokenRefreshPeriodInDays;
                }

                initialAuthCancellationToken = null;
            }

            async Task RefreshSessionToken()
            {
                if (string.IsNullOrEmpty(SessionToken))
                {
                    HandleLoginError(LoginType.SessionToken, Result.InvalidCredentials, LoginErrorType.InvalidCredentials, ErrorType.InvalidCredentials, Error.RuntimeInvalidCredentials);
                    return;
                }

                try
                {
                    var response = await requestFactory.SendRequestAsync("/session", "POST", "", null,
                        $"{nameof(AuthClient)}.{nameof(RefreshSessionToken)}", SessionToken);
                    _ = HandleLoginResponse(LoginType.SessionToken, response);
                }
                catch (RequestException ex)
                {
                    logger.Error(Error.RuntimeCloudSimulatorAuthToken,
                        ex.ToString());
                }
            }
        }
#endif
    }
}
