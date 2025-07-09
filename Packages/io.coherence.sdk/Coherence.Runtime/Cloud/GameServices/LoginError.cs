// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using Log;
    using Runtime;

    /// <summary>
    /// Represents an <see cref="AuthClient"/> error that has occurred either during an attempted login operation,
    /// or after a successful login operation, when the client's connection to coherence Cloud has been forcefully
    /// closed by the server (<see cref="ErrorType.ConcurrentConnection"/>).
    /// </summary>
    /// <seealso cref="AuthClient.OnError"/>
    public sealed class LoginError : Exception
    {
        /// <summary>
        /// Describes the type of the error.
        /// </summary>
        public ErrorType Type { get; }
        internal LoginErrorType LoginErrorType { get; }
        internal Error Error { get; }

        /// <summary>
        /// The raw json response from the server for the login request.
        /// </summary>
        internal string ResponseBody { get; }

        internal LoginError(ErrorType errorType, LoginErrorType loginErrorType, Error error, string message = "", string responseBody = "") : base(message)
        {
            Type = errorType;
            LoginErrorType = loginErrorType;
            ResponseBody = responseBody;
            Error = error;
        }

        public override string ToString() => Message is { Length: > 0 } ? Type + ": " + Message : Type.ToString();
    }
}
