// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    /// <summary>
    /// Provides multiple ways to authenticate players with the coherence Cloud, giving them access
    /// to its <see cref="PlayerAccount.Services">services</see>.
    /// </summary>
    public static class CoherenceCloud
    {
        /// <summary>
        /// Event that is raised a new operation to log in to coherence Cloud has been started.
        /// </summary>
        public static event Action<PlayerAccount> OnLoggingIn;

        /// <summary>
        /// Event that is raised when an operation to login to coherence Cloud has failed.
        /// </summary>
        public static event Action<LoginOperationError> OnLoggingInFailed;

        /// <summary>
        /// Event that is raised when an operation to login to coherence Cloud has completed successfully.
        /// </summary>
        public static event Action<PlayerAccount> OnLoggedIn;

        /// <summary>
        /// Event that is raised when a player has started logging out from coherence Cloud.
        /// </summary>
        public static event Action<PlayerAccount> OnLoggingOut;

        /// <summary>
        /// Login to coherence Cloud using a guest account.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="LoginAsGuest(CancellationToken)"/> will always return the same guest player account
        /// on the same device with the same project selected. A new <see cref="GuestId"/> is generated automatically the first time that <see cref="LoginAsGuest(CancellationToken)"/>
        /// is called with a particular project selected, and then cached and reused whenever <see cref="LoginAsGuest(CancellationToken)"/>
        /// is called again on the same device with the same project selected.
        /// </para>
        /// <para>
        /// If you have a need to login with multiple different guest player accounts
        /// on the same device (for local multiplayer purposes, for example)
        /// use <see cref="LoginAsGuest(LoginAsGuestOptions, CancellationToken)"/>.
        /// </para>
        /// <para>
        /// 'Guest Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </para>
        /// </remarks>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginAsGuest.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginAsGuest(CancellationToken cancellationToken = default)
        {
            foreach (var existingPlayerAccount in PlayerAccount.All)
            {
                if (!existingPlayerAccount.IsGuest
                    // Calling CoherenceCloud.LoginAsGuest() multiple times should return the same playerAccount.
                    // Calling CoherenceBridge.CloudService.GameServices.AuthService.LoginAsGuest()
                    // multiple times on the same bridge should return the same playerAccount.
                    // Calling CoherenceBridge.CloudService.GameServices.AuthService.LoginAsGuest()
                    // on different bridges should return a different playerAccount.
                    || existingPlayerAccount.CloudUniqueId != PlayerAccount.DefaultCloudUniqueId

                    // CloudService is needed for checking if the playerAccount has logged in,
                    // and for subscribing to the AuthClient.OnLogin event.
                    || existingPlayerAccount.Services is not { } existingServices)
                {
                    continue;
                }

                if (existingServices.IsLoggedIn)
                {
                    return new(Task.FromResult(existingPlayerAccount));
                }

                var loggedInCompletionSource = new TaskCompletionSource<PlayerAccount>();
                existingServices.AuthClient.OnLogin += _ => loggedInCompletionSource.TrySetResult(existingPlayerAccount);
                existingServices.AuthClient.OnError += error => loggedInCompletionSource.TrySetException(error);
                return new(loggedInCompletionSource.Task);
            }

            var taskCompletionSource = new TaskCompletionSource<PlayerAccount>();
            var services = CloudService.ForClient(playerAccountProvider: null, cloudUniqueId: PlayerAccount.DefaultCloudUniqueId);
            services.AuthClient.LoginAsGuest(cancellationToken)
                .Then(PlayerAccount.OnLoginAttemptCompleted(taskCompletionSource, services, cancellationToken));
            return new(taskCompletionSource.Task);
        }

        /// <summary>
        /// Login to coherence Cloud using a guest account.
        /// </summary>
        /// <remarks>
        /// A new <see cref="GuestId"/> is generated based on the provided <see cref="LoginAsGuestOptions.CloudUniqueId">unique id</see>
        /// the first time that a playerAccount logs in as a guest to a <see cref="LoginAsGuestOptions.ProjectId">particular project</see>,
        /// and then cached and reused when the playerAccount logs in as a guest again to the same project on the same device using the same unique id.
        /// </remarks>
        /// <param name="options">
        /// Options to use when logging in as a guest.
        /// <para>
        /// Providing custom options when logging in makes it possible to log in with multiple different guest player accounts on the same device.
        /// This could be useful for local multiplayer.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        public static LoginOperation LoginAsGuest(LoginAsGuestOptions options, CancellationToken cancellationToken = default)
        {
            var cloudUniqueId = options.CloudUniqueId;
            if (cloudUniqueId == CloudUniqueId.None)
            {
                cloudUniqueId = PlayerAccount.DefaultCloudUniqueId;
            }

            var projectId = options.ProjectId;
#if UNITY
            if (string.IsNullOrEmpty(projectId))
            {
                projectId = RuntimeSettings.Instance.ProjectID;
            }
#endif

            foreach (var existingPlayerAccount in PlayerAccount.All)
            {
                if (!existingPlayerAccount.IsGuest
                    // Calling CoherenceCloud.LoginAsGuest with the same options multiple times should return the same player account each time.
                    // Calling CoherenceBridge.CloudService.GameServices.AuthService.LoginAsGuest()
                    // multiple times on the same bridge should return the same player account each time.
                    // Calling CoherenceBridge.CloudService.GameServices.AuthService.LoginAsGuest()
                    // on different bridges should return a different player account for each bridge.
                    || existingPlayerAccount.CloudUniqueId != cloudUniqueId
                    || existingPlayerAccount.projectId != projectId

                    // CloudService is needed for checking if the player account has logged in,
                    // and for subscribing to the AuthClient.OnLogin event.
                    || existingPlayerAccount.Services is not { } existingServices)
                {
                    continue;
                }

                if (existingServices.IsLoggedIn)
                {
                    return new(Task.FromResult(existingPlayerAccount));
                }

                var loggedInCompletionSource = new TaskCompletionSource<PlayerAccount>();
                existingServices.AuthClient.OnLogin += _ => loggedInCompletionSource.SetResult(existingPlayerAccount);
                existingServices.AuthClient.OnError += error => loggedInCompletionSource.SetException(error);
                return new(loggedInCompletionSource.Task);
            }

            var taskCompletionSource = new TaskCompletionSource<PlayerAccount>();
            var services = CloudService.ForClient(playerAccountProvider: null, cloudUniqueId: cloudUniqueId); // TODO: add ability to inject project id used by NewPlayerAccountProvider, in case injected project id does not match one in runtime settings
            var loginInfo = LoginInfo.ForGuest(projectId, cloudUniqueId, preferLegacyLoginData: true);
            services.AuthClient.Login(loginInfo, cancellationToken)
                .Then(PlayerAccount.OnLoginAttemptCompleted(taskCompletionSource, services, cancellationToken));
            return new(taskCompletionSource.Task);
        }

        /// <summary>
        /// Login to coherence Cloud using a username and password.
        /// </summary>
        /// <remarks>
        /// 'User / Password Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="username"> Username for the player account to log into. </param>
        /// <param name="password"> Password for the player account to log into. </param>
        /// <param name="autoSignup">
        /// Should a new player account with the provided <paramref name="username"/> be created, if one doesn't already exist?
        /// </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithPassword.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithPassword(string username, string password, bool autoSignup = false, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithPassword(username, password, autoSignup), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using a <see cref="SessionToken"/>.
        /// </summary>
        /// <remarks>
        /// A session token can be acquired from <see cref="PlayerAccount.SessionToken"/> after a previous login operation.
        /// </remarks>
        /// <param name="sessionToken"> A <see cref="SessionToken"/> from a <see cref="PlayerAccount"/>. </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithSessionToken.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithSessionToken(SessionToken sessionToken, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithSessionToken(sessionToken), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using a Steam account.
        /// </summary>
        /// <remarks>
        /// 'Steam Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="ticket"> Steam ticket for the account. </param>
        /// <param name="identity">
        /// (Optional) The identifier string that was passed as a parameter to the GetAuthTicketForWebApi method
        /// of the Steamworks Web API when the ticket was created.
        /// </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithSteam.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithSteam(string ticket, string identity = null, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithSteam(ticket, identity), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using an Epic Games account.
        /// </summary>
        /// <remarks>
        /// 'Epic Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="token"> Authentication token acquired from Epic Online Services. </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithEpicGames.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithEpicGames(string token, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithEpicGames(token), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using a PlayStation Network account.
        /// </summary>
        /// <remarks>
        /// 'PSN Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="token"> A JWT ID token with link to the public certificate embedded. </param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithPlayStation.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithPlayStation(string token, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithPlayStation(token), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using an Xbox profile.
        /// </summary>
        /// <remarks>
        /// 'Xbox Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="token">Xbox Live Token provided by the Xbox Live SDK method GetTokenAndSignatureAsync.</param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithXbox.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithXbox(string token, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithXbox(token), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using a Nintendo Account.
        /// </summary>
        /// <remarks>
        /// 'Nintendo Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="token"> Nintendo Account ID as a JSON Web Token with a link to the public certificate embedded. </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithNintendo.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithNintendo(string token, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithNintendo(token), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using a custom JSON Web Token (JWT).
        /// </summary>
        /// <remarks>
        /// 'JWT Auth Enabled' must be ticked and a 'JKU Domain' or 'Public Key' provided
        /// in Project Settings on your <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="token">
        /// <para>
        /// JSON Web Token (JWT) to log in with.
        /// </para>
        /// <para>
        /// Signed with an asymmetric algorithm (such as RS256 or ES256).
        /// Must contain a valid “sub” value which is the external unique player ID.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithJwt.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithJwt(string token, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithJwt(token), cancellationToken);

        /// <summary>
        /// Login to coherence Cloud using a one-time code.
        /// </summary>
        /// <remarks>
        /// <para>
        /// There are two use cases for one-time codes:
        /// <list type="number">
        /// <item>
        /// <description> transfer progress from one platform to another, </description>
        /// </item>
        /// <item>
        /// <description> recover access to a lost account. </description>
        /// </item>
        /// </list>
        /// </para>
        /// <para>
        /// One-time codes expire after a certain time and can only be used once.
        /// </para>
        /// <para>
        /// 'One-Time Code Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </para>
        /// </remarks>
        /// <param name="code"> One-time code acquired using <see cref="PlayerAccount.GetOneTimeCode"/> . </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous login operation. </returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithOneTimeCode.cs" language="csharp"/>
        /// </example>
        public static LoginOperation LoginWithOneTimeCode(string code, CancellationToken cancellationToken = default)
            => Login(LoginInfo.WithOneTimeCode(code), cancellationToken);

        internal static LoginOperation Login(LoginInfo loginInfo, CancellationToken cancellationToken = default)
        {
            if (PlayerAccount.Find(loginInfo) is { Services: { } existingServices } existingPlayerAccount)
            {
                if (existingPlayerAccount.IsLoggedIn)
                {
                    return new(Task.FromResult(existingPlayerAccount));
                }

                var loggedInCompletionSource = new TaskCompletionSource<PlayerAccount>();
                existingServices.AuthClient.OnLogin += _ => loggedInCompletionSource.TrySetResult(existingPlayerAccount);
                existingServices.AuthClient.OnError += error => loggedInCompletionSource.TrySetException(error);
                return new(loggedInCompletionSource.Task);
            }

            var taskCompletionSource = new TaskCompletionSource<PlayerAccount>();
            var services = CloudService.ForClient();
            services.AuthClient.Login(loginInfo, cancellationToken)
                .Then(PlayerAccount.OnLoginAttemptCompleted(taskCompletionSource, services, cancellationToken));
            return new(taskCompletionSource.Task);
        }

        internal static void RaiseOnLoggingIn(PlayerAccount playerAccount) => OnLoggingIn?.Invoke(playerAccount);
        internal static void RaiseOnLoggingInFailed(LoginOperationError error) => OnLoggingInFailed?.Invoke(error);
        internal static void RaiseOnLoggedIn(PlayerAccount playerAccount) => OnLoggedIn?.Invoke(playerAccount);
        internal static void RaiseOnLoggingOut(PlayerAccount playerAccount) => OnLoggingOut?.Invoke(playerAccount);
    }
}
