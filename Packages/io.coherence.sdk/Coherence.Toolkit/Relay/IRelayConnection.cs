// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Relay
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Represents a single client connection made over external service that is relayed to the
    ///     Replication Server.
    /// </summary>
    public interface IRelayConnection
    {
        /// <summary>
        ///     Called by the <see cref="CoherenceRelayManager" /> after successful registration of the
        ///     connection via <see cref="CoherenceRelayManager.OpenRelayConnection" />.
        /// </summary>
        void OnConnectionOpened();

        /// <summary>
        ///     Called by the <see cref="CoherenceRelayManager" /> after de-registration of the connection
        ///     via <see cref="CoherenceRelayManager.CloseAndRemoveRelayConnection" />.
        /// </summary>
        void OnConnectionClosed();

        /// <summary>
        ///     Called regularly by the <see cref="CoherenceRelayManager" /> to fetch all messages
        ///     (packets) received from that connection and relay them to the Replication Server.
        /// </summary>
        /// <param name="packetBuffer">
        ///     Buffer to which received packets should be added. Order of the packets
        ///     should be preserved.
        /// </param>
        void ReceiveMessagesFromClient(List<ArraySegment<byte>> packetBuffer);

        /// <summary>
        ///     Called by the <see cref="CoherenceRelayManager" /> when there's a message (packet) from
        ///     the Replication Server that should be relayed to this connection.
        /// </summary>
        void SendMessageToClient(ReadOnlySpan<byte> packetData);
    }
}
