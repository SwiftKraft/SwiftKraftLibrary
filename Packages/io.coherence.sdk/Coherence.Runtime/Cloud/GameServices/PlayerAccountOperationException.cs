// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using Log;

    /// <summary>
    /// An exception that can occur during <see cref="IAuthClientInternal.PlayerAccountOperationAsync"/> operations.
    /// </summary>
    internal sealed class PlayerAccountOperationException : Exception
    {
        public PlayerAccountErrorType Type { get; }
        public Error Error { get; }

        internal PlayerAccountOperationException(PlayerAccountErrorType type, Error error, string message = "") : base(message)
        {
            Type = type;
            Error = error;
        }

        public override string ToString() => Message is { Length: > 0 } ? Type + ": " + Message : Type.ToString();
    }
}
