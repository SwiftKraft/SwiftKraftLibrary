// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Runtime;

    /// <summary>
    /// Specifies the types of errors that can occur when attempting to login.
    /// </summary>
    /// <seealso cref="CoherenceCloud.LoginWithPassword"/>
    public enum LoginErrorType
    {
        /// <summary>
        /// Log in operation was completed successfully.
        /// </summary>
        None = 0,

        /// <inheritdoc cref="ErrorType.ServerError"/>
        ServerError = 1,

        /// <inheritdoc cref="ErrorType.InvalidCredentials"/>
        InvalidCredentials = 2,

        /// <inheritdoc cref="ErrorType.InvalidResponse"/>
        InvalidResponse = 4,

        /// <inheritdoc cref="ErrorType.TooManyRequests"/>
        TooManyRequests = 5,

        /// <inheritdoc cref="ErrorType.AlreadyLoggedIn"/>
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

        /// <summary>
        /// Logging in failed because of invalid Online Dashboard configuration.
        /// </summary>
        InvalidConfig = 8,

        OneTimeCodeExpired = 10,
        OneTimeCodeNotFound = 11,
        ConnectionError = 12,
        IdentityLimit = 13,
        IdentityNotFound = 14,
        IdentityTaken = 16,
        IdentityTotalLimit = 17,
        InvalidInput = 18,
        PasswordNotSet = 19,
        UsernameNotAvailable = 20
    }
}
