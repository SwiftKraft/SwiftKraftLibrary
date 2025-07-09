// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime
{
    /// <summary>
    /// Specifies the types of errors that can occur in <see cref="AuthClient"/> during an attempted login operation,
    /// or after a successful login operation, when the client's connection to coherence Cloud has been forcefully
    /// closed by the server (<see cref="ErrorType.ConcurrentConnection"/>).
    /// </summary>
    /// <seealso cref="AuthClient.OnError"/>
    public enum ErrorType
    {
        /// <summary>
        /// Logging in failed because of server error.
        /// </summary>
        ServerError = 1,

        /// <summary>
        /// Logging in failed because an invalid username, password of session token was provided.
        /// </summary>
        InvalidCredentials = 2,

        /// <summary>
        /// Logging in failed because 'Persisted Player Accounts' option is not enabled.
        /// <para>
        /// You can enable the feature in your
        /// <see href="https://coherence.io/dashboard/">coherence Cloud Dashboard</see> under Project Settings.
        /// </para>
        /// </summary>
        FeatureDisabled = 3,

        /// <summary>
        /// Logging in failed because was unable to deserialize the response from the server.
        /// </summary>
        InvalidResponse = 4,

        /// <summary>
        /// Logging in failed because too many requests have been sent within a short amount of time.
        /// <para>
        /// Please slow down the rate of sending requests, and try again later.
        /// </para>
        /// </summary>
        TooManyRequests = 5,

        /// <summary>
        /// Logging in operation aborted, because already logged in.
        /// <para>
        /// You have to call <see cref="AuthClient.Logout"/> before attempting to log in again.
        /// </para>
        /// </summary>
        AlreadyLoggedIn = 6,

        /// <summary>
        /// Connection to the coherence Cloud has been forcefully closed by the server.
        /// </summary>
        /// <remarks>
        /// Usually this happens when a concurrent connection is detected,
        /// e.g. running multiple game clients for the same player.
        /// When this happens the game should present a prompt to the player
        /// to inform them that there is another instance of the game running.
        /// The game should wait for player input and never try to reconnect on its own
        /// or else the two game clients would disconnect each other indefinitely.
        /// </remarks>
        ConcurrentConnection = 7,
        InvalidConfig = 8,
        InvalidApp = 9,
        OneTimeCodeExpired = 10,
        OneTimeCodeNotFound = 11,
        ConnectionError = 12,
        IdentityLimit = 13,
        IdentityNotFound = 14,
        IdentityTaken = 16,
        IdentityTotalLimit = 17,
        InvalidInput = 18,
        PasswordNotSet = 19,
        UsernameNotAvailable = 20,
    }
}
