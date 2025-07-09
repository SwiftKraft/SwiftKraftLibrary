// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;
    using Brisk;
    using Coherence.Core;
    using Common;
    using Connection;
    using Entities;
    using ProtocolDef;
    using Transport;

    public interface IClient : IDisposable
    {
        event Action<ClientID> OnConnected;
        event Action<ConnectionCloseReason> OnDisconnected;
        event Action<ConnectionException> OnConnectionError;
        event Action<EndpointData> OnConnectedEndpoint;
        event Action<ConnectionValidationRequest> OnValidateConnectionRequest;

        event Action<Entity, IncomingEntityUpdate> OnEntityCreated;
        event Action<Entity, IncomingEntityUpdate> OnEntityUpdated;
        event Action<Entity, DestroyReason> OnEntityDestroyed;

        event Action<IEntityCommand, MessageTarget, Entity> OnCommand;
        event Action<IEntityInput, long, Entity> OnInput;

        event Action<AuthorityRequest> OnAuthorityRequested;
        event Action<AuthorityRequestRejection> OnAuthorityRequestRejected;
        event Action<AuthorityChange> OnAuthorityChange;
        event Action<Entity> OnAuthorityTransferred;

        event Action<SceneIndexChanged> OnSceneIndexChanged;

        ClientID ClientID { get; }
        INetworkTime NetworkTime { get; }
        ConnectionType ConnectionType { get; }
        string Hostname { get; }
        Stats.Stats Stats { get; }
        ConnectionState ConnectionState { get; }
        Ping Ping { get; }
        EndpointData LastEndpointData { get; }
        ConnectionSettings ConnectionSettings { get; }
        uint InitialScene { get; set; }

        /// <inheritdoc cref="IConnection.ValidatedHostPayload"/>
        CustomPayload ValidatedHostPayload { get; }

        /// <inheritdoc cref="IConnection.ConnectionValidationPayload"/>
        CustomPayload ConnectionValidationPayload { get; set; }

        byte SendFrequency { get; }

        void Connect(EndpointData data, ConnectionSettings connectionSettings, ConnectionType connectionType = ConnectionType.Client);

        bool IsConnected();
        bool IsDisconnected();
        bool IsConnecting();
        void UpdateReceiving();
        void UpdateSending();
        void Disconnect();
        void Reconnect();

        void KickConnection(ClientID clientID, CustomPayload hostPayload);

        Entity CreateEntity(ICoherenceComponentData[] data, bool orphan);
        bool CanSendUpdates(Entity id);
        void UpdateComponents(Entity id, ICoherenceComponentData[] data);
        void RemoveComponents(Entity id, uint[] data);
        void DestroyEntity(Entity id);

        bool EntityExists(Entity id);

        public bool HasAuthorityOverEntity(Entity entity, AuthorityType authorityType);
        public bool IsEntityInAuthTransfer(Entity id);

        void SendCommand(IEntityCommand message, MessageTarget target, Entity id, ChannelID channelID);
        void SendInput(IEntityInput message, long frame, Entity id);

        void SendAuthorityRequest(Entity id, AuthorityType authorityMode = AuthorityType.Full);

        void SendAdoptOrphanRequest(Entity id);

        bool SendAuthorityTransfer(Entity id, ClientID newAuthority, bool authorized, AuthorityType authorityType = AuthorityType.Full);

        void SetFloatingOrigin(Vector3d newFloatingOrigin);
        Vector3d GetFloatingOrigin();

        void SetTransportType(TransportType transportType, TransportConfiguration transportConfiguration);
        void SetTransportFactory(ITransportFactory transportFactory);

        // For debug testing only.
        string DebugGetTransportDescription();
        void DebugHoldAllPackets(bool hold);
        void DebugReleaseAllHeldPackets();
        void DebugSetNetworkCondition(Condition condition);
        void DebugStopSerializingUpdates(bool stop);
        void DebugDropNextOutPacket(Action callback);
        void DebugOnNextPacketSentOneShot(Action callback);
        event Action<Entity> DebugOnEntityAcked;
        event Action<PacketSentDebugInfo> DebugOnPacketSent;
        event Action<int> DebugOnPacketReceived;
    }
}
