// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;

    /// <summary>
    /// All the messages that are present in the Coherence protocol.
    /// </summary>
    public enum MessageType
    {
        EcsWorldUpdate = 0x08,
        // Unused = 0x09,
        // Unused = 0x10,
        Command = 0x11,
        // Unused = 0x12,
        Input = 0x13,
        EndOfMessages = 0xFF,
    }

    public static class MessageTypeExtensions
    {
        public static string AsString(this MessageType messageType) =>
            messageType switch
            {
                MessageType.EcsWorldUpdate => nameof(MessageType.EcsWorldUpdate),
                MessageType.Command => nameof(MessageType.Command),
                MessageType.Input => nameof(MessageType.Input),
                MessageType.EndOfMessages => nameof(MessageType.EndOfMessages),
                _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null),
            };
    }
}
