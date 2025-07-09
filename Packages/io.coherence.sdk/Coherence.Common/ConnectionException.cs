// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using System;
    using Coherence.Common;

    public class ConnectionException : Exception
    {
        public ConnectionException(string message) : base(message) { }
        public ConnectionException(string message, Exception innerException) : base(message, innerException) { }

        public override string Message => InnerException == null ? base.Message : $"{InnerException.Message}: {base.Message}";

        public virtual (string title, string message) GetPrettyMessage()
        {
            return ("Error connecting", Message);
        }
    }

    public class ConnectionClosedException : ConnectionException
    {
        // The InnerException is likely a TCP SocketException with an error code of
        // SocketError.ConnectionReset or SocketError.Shutdown.
        public ConnectionClosedException(string message, Exception innerException) : base(message, innerException) { }

        public override (string title, string message) GetPrettyMessage()
        {
            return ("Connection closed unexpectedly", Message);
        }
    }

    public class ConnectionTimeoutException : ConnectionException
    {
        public TimeSpan After { get; }

        public override string Message => $"{base.Message}, Timeout: {After:g}";

        public ConnectionTimeoutException(TimeSpan after)
            : this(after, null, null) { }

        public ConnectionTimeoutException(TimeSpan after, string message)
            : this(after, message, null) { }

        public ConnectionTimeoutException(TimeSpan after, string message, Exception innerException)
            : base(message, innerException)
        {
            After = after;
        }

        public override (string title, string message) GetPrettyMessage()
        {
            return ("Connection timed out", $"Connection timed out after {After:g}.");
        }
    }

    public class ConnectionDeniedException : ConnectionException
    {
        public ConnectionCloseReason CloseReason { get; }
        public CustomPayload HostPayload { get; }

        public override string Message
        {
            get
            {
                string message = base.Message;

                return !string.IsNullOrEmpty(message)
                    ? $"{base.Message}, DenyReason: {CloseReason}, HasPayload: {!HostPayload.IsEmpty}"
                    : $"DenyReason: {CloseReason}, HasPayload: {!HostPayload.IsEmpty}";
            }
        }

        public ConnectionDeniedException(ConnectionCloseReason closeReason)
            : this(closeReason, CustomPayload.Empty, null, null) { }

        public ConnectionDeniedException(ConnectionCloseReason closeReason, CustomPayload hostPayload)
            : this(closeReason, hostPayload, null, null) { }

        public ConnectionDeniedException(ConnectionCloseReason closeReason, CustomPayload hostPayload, string message)
            : this(closeReason, hostPayload, message, null) { }

        public ConnectionDeniedException(ConnectionCloseReason closeReason, CustomPayload hostPayload, string message, Exception innerException)
            : base(message, innerException)
        {
            CloseReason = closeReason;
            HostPayload = hostPayload;
        }

        public override (string title, string message) GetPrettyMessage()
        {
            switch (CloseReason)
            {
                case ConnectionCloseReason.HostNotReady:
                    return ("Host not ready", "The room or world you are trying to join has HostAuthority features enabled, " +
                                        "but no simulator is connected yet.");
                case ConnectionCloseReason.ConnectionRejectedByHost:
                    return ("Connection rejected by host", "The host or simulator has rejected your connection request.");
                case ConnectionCloseReason.ConnectionValidationTimeout:
                    return ("Connection validation timeout", "The host or simulator took too long to validate your connection request.");
                case ConnectionCloseReason.KickedByHost:
                    return ("Kicked by host", "You were kicked from the room by the host or simulator.");
                case ConnectionCloseReason.HostDisconnected:
                    return ("Host disconnected", "The host or simulator has disconnected. Since HostAuthority features are enabled, " +
                                        "all clients were kicked because there's no host anymore.");
            }

            return ("Connection denied", Message);
        }
    }
}
