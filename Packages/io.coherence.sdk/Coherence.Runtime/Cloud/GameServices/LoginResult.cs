// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Log;
    using Runtime;

    /// <summary>
    /// Represents the result of an <see cref="IAuthClient"/> login operation.
    /// </summary>
    /// <seealso cref="AuthClient.LoginAsGuest"/>
    /// <seealso cref="AuthClient.LoginWithPassword"/>
    /// <seealso cref="AuthClient.LoginWithSessionToken"/>
    /// <seealso cref="AuthClient.OnLogin"/>
    public record LoginResult
    {
        /// <summary>
        /// The player account that was successfully logged in, or <see langword="PlayerAccount.None"/> if the login operation failed.
        /// </summary>
        public PlayerAccount PlayerAccount { get; }

        /// <summary>
        /// Describes the type of the result.
        /// <para>
        /// <see cref="Result.Success"/> if the login operation was successful; otherwise, the type of login failure.
        /// </para>
        /// </summary>
        public Result Type { get; }

        /// <summary>
        /// Identifier for the logged-in player account, if the login operation was successful; otherwise, an empty string.
        /// </summary>
        public PlayerAccountId Id { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use " + nameof(Id) + " instead.")]
        [Deprecated("04/2025", 1, 6, 0, Reason = "Use " + nameof(Id) + " instead.")]
        public string UserId => Id;

        /// <summary>
        /// Username of the player account, if one has been associated with it; otherwise, an empty string.
        /// </summary>
        public string Username => PlayerAccount?.Username ?? "";

        /// <summary>
        /// Automatically generated random password for the player account, if successfully
        /// <see cref="AuthClient.LoginAsGuest"> logged in as a guest</see>; otherwise, an empty string.
        /// </summary>
        [Obsolete("Guest users no longer have passwords. This property will be removed in a future version"), Deprecated("03/2025", 1, 6, 0, Reason = "Guest users no longer have passwords.")]
        public string GuestPassword => "";

        /// <summary>
        /// Token uniquely identifying the logged-in player account.
        /// <para>
        /// The token can be stored on the user's device locally, and later used to
        /// <see cref="CoherenceCloud.LoginWithSessionToken(Coherence.Cloud.SessionToken, System.Threading.CancellationToken)">log in</see>
        /// to coherence Cloud again using the same credentials, without the user needing to provide them again.
        /// </para>
        /// </summary>
        public SessionToken SessionToken => response is null ? SessionToken.None : new(response.Value.SessionToken);

        public ErrorType ErrorType { get; }
        public string ErrorMessage { get; }

        public IReadOnlyList<KeyValuePair<string, string>> KeyValuePairStoreState { get; }
        public IReadOnlyList<string> LobbyIds { get; }

        /// <summary>
        /// <see langword="true"/> if the login operation was successful, or if it was cancelled because
        /// the client is <see cref="Result.AlreadyLoggedIn">already logged in</see>; otherwise, <see langword="false"/>.
        /// </summary>
        public bool LoggedIn => Type is Result.Success or Result.AlreadyLoggedIn;

        internal LoginErrorType LoginErrorType { get; }
        internal Error Error { get; }

        internal readonly LoginResponse? response;

        internal static LoginResult Success(PlayerAccount playerAccount, Result type, LoginResponse response) => new(type, playerAccount, response, null);
        internal static LoginResult Failure(Result type, LoginError error) => new(type, null, null, error);

        private LoginResult(Result type, PlayerAccount playerAccount, LoginResponse? response, LoginError error)
        {
            PlayerAccount = playerAccount;
            Id = response?.Id ?? PlayerAccountId.None;
            Type = type;
            KeyValuePairStoreState = response?.KvStoreState?
                .Select(r => new KeyValuePair<string, string>(r.Key, r.Value))
                .ToArray() ?? Array.Empty<KeyValuePair<string, string>>();
            LobbyIds = (IReadOnlyList<string>)(response?.LobbyIds) ?? Array.Empty<string>();
            this.response = response;
            ErrorMessage = error?.Message ?? "";
            LoginErrorType = error?.LoginErrorType ?? LoginErrorType.None;
            ErrorType = error?.Type ?? (ErrorType)-1;
            Error = error?.Error ?? (Error)-1;
        }

        public static implicit operator Result(LoginResult loginResult) => loginResult.Type;
        public static bool operator ==(LoginResult result, Result type) => result?.Type == type;
        public static bool operator !=(LoginResult result, Result type) => result?.Type != type;
        public bool Equals(Result type) => Type == type;
        public override string ToString() => ErrorMessage is { Length: > 0 } ? Type + ": " + ErrorMessage : Type.ToString();
    }
}
