// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    /// <summary>
    /// Specifies the types of errors that can occur when attempting to perform
    /// operations related to a <see cref="PlayerAccount"/>.
    /// </summary>
    public enum PlayerAccountErrorType
    {
        InternalException,
        NotLoggedIn,
        ServerError,
        InvalidConfig,
        InvalidApp,
        FeatureDisabled,
        InvalidCredentials,
        InvalidResponse,
        ConnectionError,
        TooManyRequests,
        ConcurrentConnection,
        OneTimeCodeExpired,
        OneTimeCodeNotFound,
        IdentityLimit,
        IdentityNotFound,
        IdentityRemoval,
        IdentityTaken,
        IdentityTotalLimit,
        InvalidInput,
        PasswordNotSet,
        UsernameNotAvailable
    }
}
