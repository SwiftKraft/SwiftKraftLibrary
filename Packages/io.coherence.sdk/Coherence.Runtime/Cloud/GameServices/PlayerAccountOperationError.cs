// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Common;
    using Log;

    /// <summary>
    /// Represents an error that has occurred when attempting to perform operations related
    /// to a <see cref="PlayerAccount"/>.
    /// </summary>
    /// <remarks>
    /// If the <see cref="PlayerAccountOperation.Error"/> error is never observed by the user,
    /// it will get automatically logged to the Console.
    /// </remarks>
    public sealed class PlayerAccountOperationError : CoherenceError<PlayerAccountErrorType>
    {
        internal PlayerAccountOperationError(PlayerAccountErrorType errorType, Error error, string message, bool hasBeenObserved = false) : base(errorType, message, error, hasBeenObserved) { }
        internal PlayerAccountOperationError(PlayerAccountOperationException exception, bool hasBeenObserved = false) : base(exception.Type, exception.Message, exception.Error, hasBeenObserved) { }
    }
}
