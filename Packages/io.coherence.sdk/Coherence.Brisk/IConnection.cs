// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using Brisk;
    using Brisk.Models;
    using Brook;
    using Common;
    using System;
    using System.Collections.Generic;

    internal interface IOutConnection
    {
        bool CanSend { get; }
        bool UseDebugStreams { get; }
        OutPacket CreatePacket(bool reliable);
        void Send(OutPacket packet);
    }

    internal interface IConnection : IOutConnection
    {
        event Action<ConnectResponse> OnConnect;
        event Action<ConnectionCloseReason> OnDisconnect;
        event Action<ConnectionException> OnError;
        event Action<DeliveryInfo> OnDeliveryInfo;
        event Action<ConnectionValidationRequest> OnValidateConnectionRequest;

        ClientID ClientID { get; }
        ConnectionState State { get; }
        Ping Ping { get; }
        byte SendFrequency { get; }
        uint InitialScene { get; set; }

        /// <summary>
        /// Payload received from the host or simulator after it accepted our connection.
        /// </summary>
        CustomPayload ValidatedHostPayload { get; }

        /// <summary>
        /// Payload that will be sent to the host or simulator to validate the connection.
        /// </summary>
        CustomPayload ConnectionValidationPayload { get; set; }
        string TransportDescription { get; }

        void Update();

        void Connect(EndpointData data, ConnectionType connectionType, ConnectionSettings settings);
        void Disconnect(ConnectionCloseReason connectionCloseReason, bool serverInitiated);

        void SendKickRequest(ClientID clientID, CustomPayload hostPayload);

        void Receive(List<InPacket> buffer);
    }
}
