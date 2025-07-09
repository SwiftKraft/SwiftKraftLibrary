// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Runtime;
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Specifies a set of methods that can be used to log in to coherence Cloud, properties to determine
    /// if we are currently logged in or not, and events for getting notified about relevant things happening.
    /// </summary>
    public interface IAuthClient
    {
        /// <summary>
        /// Event that is invoked when the client has successfully logged in to coherence Cloud.
        /// </summary>
        event Action<LoginResponse> OnLogin;

        /// <summary>
        /// Event that is invoked when the client has <see cref="Logout">logged out</see> from coherence Cloud.
        /// </summary>
        event Action OnLogout;

        /// <summary>
        /// Event that is invoked when a login request has failed, and when the client's connection to
        /// coherence Cloud has been forcefully closed by the server.
        /// <para>
        /// <see cref="LoginError.Type"/> describes the type of the failure.
        /// </para>
        /// </summary>
        event Action<LoginError> OnError;

        /// <summary>
        /// Is the client currently logged in to coherence Cloud?
        /// </summary>
        bool LoggedIn { get; }

        /// <summary>
        /// Login to coherence Cloud using a guest account, without providing any username or password.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A new <see cref="GuestId"/> is generated automatically the first time that <see cref="LoginAsGuest(CancellationToken)"/>
        /// is called with a particular project selected, and then cached and reused whenever <see cref="LoginAsGuest(CancellationToken)"/>
        /// is called again on the same device with the same project selected.
        /// </para>
        /// <para>
        /// 'Guest Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </para>
        /// </remarks>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns>The status of the asynchronous login operation.</returns>
        Task<LoginResult> LoginAsGuest(CancellationToken cancellationToken = default);

        /// <summary>
        /// Login to coherence Cloud using a specific account with a username and password.
        /// </summary>
        /// <remarks>
        /// 'User / Password Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="username"> Username for the account which is used to log in. </param>
        /// <param name="password"> Password for the account which is used to log in. </param>
        /// <param name="autoSignup">
        /// Should an account with the provided <paramref name="username"/> be created, if one doesn't already exist?
        /// </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns>The status of the asynchronous login operation.</returns>
        Task<LoginResult> LoginWithPassword(string username, string password, bool autoSignup, CancellationToken cancellationToken = default);

        /// <summary>
        /// Login to coherence Cloud using a one-time code.
        /// </summary>
        /// <remarks>
        /// <para>
        /// There are two use cases for one-time codes:
        /// <list type="number">
        /// <item>
        /// <description>transfer progress from one platform to another,</description>
        /// </item>
        /// <item>
        /// <description>recover access to a lost account.</description>
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
        /// <returns>The status of the asynchronous login operation.</returns>
        Task<LoginResult> LoginWithOneTimeCode(string code, CancellationToken cancellationToken = default);

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
        /// <returns>The status of the asynchronous login operation.</returns>
        public Task<LoginResult> LoginWithSteam(string ticket, string identity = null, CancellationToken cancellationToken = default);

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
        /// <returns>The status of the asynchronous login operation.</returns>
        public Task<LoginResult> LoginWithEpicGames(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Login to coherence Cloud using a PlayStation Network account.
        /// </summary>
        /// <remarks>
        /// 'PSN Auth Enabled' must be ticked in Project Settings on your
        /// <see href="https://coherence.io/dashboard">Online Dashboard</see>
        /// for this authentication method to be usable.
        /// </remarks>
        /// <param name="token"> A JWT ID token with link to the public certificate embedded. </param>
        /// <returns>The status of the asynchronous login operation.</returns>
        public Task<LoginResult> LoginWithPlayStation(string token, CancellationToken cancellationToken = default);

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
        /// <returns>The status of the asynchronous login operation.</returns>
        public Task<LoginResult> LoginWithXbox(string token, CancellationToken cancellationToken = default);

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
        /// <returns>The status of the asynchronous login operation.</returns>
        public Task<LoginResult> LoginWithNintendo(string token, CancellationToken cancellationToken = default);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will be removed in a future version. Use "+ nameof(LoginWithSessionToken) + " instead.")]
        [Deprecated("03/2025", 1, 6, 0, Reason="Renamed to 'LoginWithSessionToken' to avoid ambiguity with the JSON Web Token based authentication.")]
        Task<LoginResult> LoginWithToken(SessionToken sessionToken) => LoginWithSessionToken(sessionToken);

        /// <summary>
        /// Login to coherence Cloud using a <see cref="SessionToken"/> acquired from the result of a previous login operation.
        /// </summary>
        /// <param name="sessionToken">
        /// A <see cref="SessionToken"/> acquired from the result of a previous login operation.
        /// </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns>The status of the asynchronous login operation.</returns>
        /// <seealso cref="PlayerAccount.SessionToken"/>
        Task<LoginResult> LoginWithSessionToken(SessionToken sessionToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Login to coherence Cloud using a custom JSON Web Token (JWT).
        /// </summary>
        /// <remarks>
        /// 'JWT Auth Enabled' must be ticked and 'JKU Domain' and 'Public Key' configured
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
        /// <returns>The status of the asynchronous login operation.</returns>
        Task<LoginResult> LoginWithJwt(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear the cached Login credentials and be considered as logged out from the coherence Cloud.
        /// </summary>
        void Logout();
    }
}
