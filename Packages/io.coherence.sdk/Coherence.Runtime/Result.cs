// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Cloud;

    /// <summary>
    /// Specifies the different types of outcomes that can occur when attempting to log in using <see cref="IAuthClient"/>.
    /// </summary>
    /// <seealso cref="LoginResult"/>
    public enum Result
    {
        /// <summary>
        /// Log in operation was completed successfully.
        /// </summary>
        Success = 1,

        /// <inheritdoc cref="ErrorType.ServerError"/>
        ServerError = 2,

        /// <inheritdoc cref="ErrorType.InvalidCredentials"/>
        InvalidCredentials = 3,

        /// <inheritdoc cref="ErrorType.FeatureDisabled"/>
        FeatureDisabled = 4,

        /// <inheritdoc cref="ErrorType.InvalidResponse"/>
        InvalidResponse = 5,

        /// <inheritdoc cref="ErrorType.TooManyRequests"/>
        TooManyRequests = 6,

        /// <inheritdoc cref="ErrorType.AlreadyLoggedIn"/>
        AlreadyLoggedIn = 7,
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

    internal static class ResultUtils
    {
        public static Result? ErrorToResult(ErrorCode error) => error switch
        {
            ErrorCode.InvalidCredentials => Result.InvalidCredentials,
            ErrorCode.TooManyRequests => Result.TooManyRequests,
            ErrorCode.FeatureDisabled => Result.FeatureDisabled,
            ErrorCode.OneTimeCodeExpired => Result.OneTimeCodeExpired,
            ErrorCode.LoginNotFound => Result.OneTimeCodeExpired,
            _ => null
        };
    }
}
