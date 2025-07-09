// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Common;
    using Log;

    /// <summary>
    /// Represents an error that occurred when attempting to log in to
    /// <see cref="CoherenceCloud">coherence Cloud</see>.
    /// <para>
    /// If the <see cref="LoginOperation.Error">error</see> is never observed by the user,
    /// it will get automatically logged to the Console.
    /// </para>
    /// </summary>
    public sealed class LoginOperationError : CoherenceError<LoginErrorType>
    {
        internal LoginOperationError(LoginErrorType type, Error error = Error.UnobservedError, bool hasBeenObserved = false) : base(type, error, hasBeenObserved) { }
        internal LoginOperationError(LoginErrorType type, string message, Error error = Error.UnobservedError, bool hasBeenObserved = false) : base(type, message, error, hasBeenObserved) { }
    }
}
