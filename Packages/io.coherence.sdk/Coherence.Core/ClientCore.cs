// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Brisk;
    using Brisk.Models;
    using Brook;
    using Coherence.Core.Channels;
    using Common;
    using Connection;
    using Entities;
    using Log;
    using ProtocolDef;
    using SimulationFrame;
    using Stats;
    using Transport;

    public static class Core
    {
        public static IClient GetNewClient(
            IDefinition root,
            Logger logger,
            HashSet<Entity> activeEntities = null,
            TransportType transportType = TransportType.UDPWithTCPFallback,
            TransportConfiguration transportConfiguration = TransportConfiguration.Default,
            BriskServices briskServices = null
        )
        {
            var transportFactory = new DefaultTransportFactory(transportType, transportConfiguration);
            return GetNewClient(root, logger, activeEntities, transportFactory, briskServices);
        }

        public static IClient GetNewClient(
            IDefinition root,
            Logger logger,
            HashSet<Entity> activeEntities = null,
            ITransportFactory transportFactory = null,
            BriskServices briskServices = null
        )
        {
            return new ClientCore(root, logger, activeEntities, briskServices, transportFactory);
        }
    }

#pragma warning disable 618
    internal class ClientCore : IClient
#pragma warning restore 618
    {
        private const int ServerParticipantID = 0;

        public event Action<ClientID> OnConnected;

        public event Action<ConnectionCloseReason> OnDisconnected;
        public event Action<ConnectionException> OnConnectionError;
        public event Action<EndpointData> OnConnectedEndpoint;
        public event Action<ConnectionValidationRequest> OnValidateConnectionRequest;

        public event Action<Entity, IncomingEntityUpdate> OnEntityCreated;
        public event Action<Entity, IncomingEntityUpdate> OnEntityUpdated;
        public event Action<Entity, DestroyReason> OnEntityDestroyed;

        public event Action<IEntityCommand, MessageTarget, Entity> OnCommand;
        public event Action<IEntityInput, long, Entity> OnInput;

        public event Action<PacketSentDebugInfo> DebugOnPacketSent;
        public event Action<int> DebugOnPacketReceived;
        public event Action<Entity> DebugOnEntityAcked;

        public event Action<AuthorityRequest> OnAuthorityRequested;
        public event Action<AuthorityRequestRejection> OnAuthorityRequestRejected;
        public event Action<AuthorityChange> OnAuthorityChange;
        public event Action<Entity> OnAuthorityTransferred;

        public event Action<SceneIndexChanged> OnSceneIndexChanged;

        private NetworkTime networkTime;
        public INetworkTime NetworkTime => networkTime;

        public ConnectionType ConnectionType { get; private set; }
        public ClientID ClientID => connection.ClientID;

        public string Hostname => hostname;
        public Stats Stats { get; private set; }
        public ConnectionState ConnectionState => connection.State;
        public Ping Ping => connection.Ping;
        public EndpointData LastEndpointData => lastConnectData;
        public ConnectionSettings ConnectionSettings => lastConnectionSettings;
        public uint InitialScene { get => connection.InitialScene; set => connection.InitialScene = value; }

        /// <inheritdoc/>
        public CustomPayload ValidatedHostPayload => connection.ValidatedHostPayload;

        /// <inheritdoc/>
        public CustomPayload ConnectionValidationPayload { get => connection.ConnectionValidationPayload; set => connection.ConnectionValidationPayload = value; }

        public byte SendFrequency => connection.SendFrequency;
        private readonly HashSet<Entity> knownEntities = new HashSet<Entity>(Entity.DefaultComparer);
        private readonly HashSet<Entity> ackedEntities = new HashSet<Entity>(Entity.DefaultComparer);
        private readonly Dictionary<Entity, AuthorityType> authorityByEntity = new Dictionary<Entity, AuthorityType>(Entity.DefaultComparer);

        private readonly IConnection connection;
        private readonly InConnection inConnection;
        private readonly OutConnection outConnection;
        private readonly IDefinition protocolDefinition;
        private readonly EntityIDGenerator entityIdGenerator;
        private string hostname;
        private readonly Logger logger;
        private readonly List<InPacket> receiveBuffer = new List<InPacket>();

        private ITransportFactory transportFactory;
        private TransportConditioner networkConditioner;
        private readonly TransportConditioner.Configuration networkConditionerConfig = new TransportConditioner.Configuration();

        private IDomainNameResolver domainNameResolver;
        private CancellationTokenSource dnsResolveCancellationSource;

        private EndpointData lastConnectData;
        private ConnectionType lastConnectionType = ConnectionType.Client;
        private ConnectionSettings lastConnectionSettings;

        private Vector3d floatingOrigin;

        public ClientCore(IDefinition protocolDefinition,
            Logger logger,
            HashSet<Entity> activeEntities = null,
            BriskServices briskServices = null,
            ITransportFactory transportFactory = null,
            IDomainNameResolver domainNameResolver = null)
        {
            this.logger = logger.With<ClientCore>();
            this.Stats = new Stats();
            this.networkTime = new NetworkTime(logger);

            this.protocolDefinition = protocolDefinition;
            entityIdGenerator = new EntityIDGenerator(Entity.ClientInitialIndex, Entity.MaxID, Entity.Relative, this.logger);

            this.transportFactory = transportFactory ?? new DefaultTransportFactory();
            this.domainNameResolver = domainNameResolver ?? new DomainNameResolver();

            briskServices ??= BriskServices.Default;
            briskServices.TransportFactory = SetUpTransport;

            var brisk = new Brisk(logger, briskServices);
            connection = brisk;

            var entityRegistry = new EntityRegistry(knownEntities);
            inConnection = new InConnection(
                entityRegistry,
                new Dictionary<ChannelID, IInNetworkChannel>() {
                    { ChannelID.Default, new InNetworkChannel(protocolDefinition, protocolDefinition, entityRegistry, this.Stats, this.logger) },
                    { ChannelID.Ordered, new InOrderedNetworkChannel(protocolDefinition, protocolDefinition, entityRegistry, this.Stats, this.logger) }
                },
                this.logger
            );
            inConnection.OnPacketReceived += octetsReceived => DebugOnPacketReceived?.Invoke(octetsReceived);

            outConnection = new OutConnection(
                connection,
                new Dictionary<ChannelID, IOutNetworkChannel>()
                {
                    { ChannelID.Default, new OutNetworkChannel(protocolDefinition, protocolDefinition, this.Stats, this.logger) },
                    { ChannelID.Ordered, new OutOrderedNetworkChannel(protocolDefinition, protocolDefinition, this.Stats, this.logger) }
                },
                ackedEntities,
                this.logger
            );
            outConnection.OnPacketSent += packetSentDebugInfo => DebugOnPacketSent?.Invoke(packetSentDebugInfo);
            outConnection.OnEntityAcked += entity => DebugOnEntityAcked?.Invoke(entity);
            outConnection.OnAuthorityTransferred += RaiseOnAuthorityTransferred;

            connection.OnConnect += OnConnect;
            connection.OnDisconnect += OnDisconnect;
            connection.OnError += HandleError;
            connection.OnValidateConnectionRequest += HandleOnValidateConnectionRequest;
        }

        private ITransport SetUpTransport(Logger transportLogger)
        {
            var transport = transportFactory.Create(Brisk.MaxMTU, Stats, transportLogger);
            networkConditioner = new TransportConditioner(transport, SystemDateTimeProvider.Instance, transportLogger);
            networkConditioner.SetConfiguration(networkConditionerConfig);
            return networkConditioner;
        }

        public void Connect(EndpointData data, ConnectionSettings connectionSettings, ConnectionType connectionType = ConnectionType.Client)
        {
            if (data.region == EndpointData.LocalRegion && !data.customLocalToken)
            {
                data.authToken = AuthToken.ForLocalDevelopment(connectionType);
            }

            (bool valid, string msg) = data.Validate();
            if (!valid)
            {
                logger.Warning(Warning.CoreClientConnectFailed, ("message", msg));
                return;
            }

            if (ConnectionState != ConnectionState.Disconnected)
            {
                logger.Warning(Warning.CoreClientConnectFailed, ("message", "already connected"));
                return;
            }

            logger.Debug(nameof(Connect),
                ("host", data.host),
                ("port", data.port),
                ("region", data.region),
                ("roomId", data.roomId),
                ("roomUid", data.uniqueRoomId),
                ("worldId", data.worldId),
                ("schemaId", data.schemaId));

            connection.OnDeliveryInfo += HandleDeliveryInfo;

            inConnection.OnEntityUpdate += OnEntityUpdates;
            inConnection.OnCommand += HandleCommand;
            inConnection.OnInput += HandleInput;
            inConnection.OnServerSimulationFrameReceived += OnServerSimulationFrameReceived;
            inConnection.SetMaximumTransmissionUnit(connectionSettings?.Mtu ?? Brisk.DefaultMTU);

            ConnectionType = connectionType;
            hostname = data.GetHostAndPort();

            lastConnectData = data;
            lastConnectionType = connectionType;
            lastConnectionSettings = connectionSettings;

            if (UseDnsResolution())
            {
                DnsResolveHostAndStartConnection(data, connectionSettings, connectionType);
            }
            else
            {
                StartConnection(data, connectionSettings, connectionType);
            }
        }

        private void DnsResolveHostAndStartConnection(
            EndpointData data,
            ConnectionSettings connectionSettings,
            ConnectionType connectionType)
        {
            dnsResolveCancellationSource = new CancellationTokenSource();

            domainNameResolver.Resolve(data.host, dnsResolveCancellationSource.Token, logger, ip =>
            {
                data.host = ip.ToString();

                StartConnection(data, connectionSettings, connectionType);
            }, Disconnect);
        }

        private void StartConnection(
            EndpointData data,
            ConnectionSettings connectionSettings,
            ConnectionType connectionType)
        {
            connection.Connect(data, connectionType, connectionSettings);

            RaiseOnConnectedEndpoint();
        }

        public void Reconnect()
        {
            Connect(lastConnectData, lastConnectionSettings, lastConnectionType);
        }

        public void KickConnection(ClientID clientID, CustomPayload hostPayload)
        {
            connection.SendKickRequest(clientID, hostPayload);
        }

        private void Reset()
        {
            logger.Trace("Reset");

            connection.OnDeliveryInfo -= HandleDeliveryInfo;

            inConnection.OnEntityUpdate -= OnEntityUpdates;
            inConnection.OnCommand -= HandleCommand;
            inConnection.OnInput -= HandleInput;
            inConnection.OnServerSimulationFrameReceived -= OnServerSimulationFrameReceived;
            inConnection.Clear();

            if (dnsResolveCancellationSource != null)
            {
                try
                {
                    dnsResolveCancellationSource.Cancel();
                }
                catch (Exception e)
                {
                    logger.Error(Error.CoreClientDNSResolveException, ("exception", e));
                }
            }

            NetworkTime.Reset(notify: false);

            foreach (var kv in authorityByEntity)
            {
                if (kv.Value.Contains(AuthorityType.State))
                {
                    outConnection.DestroyEntity(kv.Key);
                    outConnection.ClearAllChangesForEntity(kv.Key);
                }
            }

            outConnection.Reset();
            entityIdGenerator.Reset();

            authorityByEntity.Clear();
            knownEntities.Clear();
            ackedEntities.Clear();
        }

        private void OnConnect(ConnectResponse connectResponse)
        {
            logger.Trace(nameof(OnConnect), ("state", ConnectionState), ("clientID", ClientID));

            var simulationFrame = new AbsoluteSimulationFrame { Frame = (long)connectResponse.SimulationFrame };
            networkTime.SetServerSimulationFrame(simulationFrame, connection.Ping);

            // Update the connection settings with the negotiated MTU.
            lastConnectionSettings.Mtu = connectResponse.MTU;

            try
            {
                OnConnected?.Invoke(ClientID);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnConnect)),
                    ("exception", handlerException));
            }
        }

        private void OnDisconnect(ConnectionCloseReason reason)
        {
            logger.Trace(nameof(OnDisconnect), ("state", ConnectionState), ("clientID", ClientID));

            try
            {
                OnDisconnected?.Invoke(reason);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnDisconnect)),
                    ("exception", handlerException));
            }
        }

        private void HandleOnValidateConnectionRequest(ConnectionValidationRequest request)
        {
            if (OnValidateConnectionRequest != null)
            {
                OnValidateConnectionRequest.Invoke(request);
            }
            else
            {
                // In case there's no handler, we accept the connection by default.
                request.Respond(new ConnectionValidationResponse(true));
            }
        }

        private void HandleError(ConnectionException exception)
        {
            logger.Debug(nameof(HandleError), ("state", ConnectionState), ("exception", exception));

            try
            {
                OnConnectionError?.Invoke(exception);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(HandleError)),
                    ("exception", handlerException));

            }

            ConnectionCloseReason disconnectReason = ConnectionCloseReason.Unknown;
            bool serverInitiated = false;
            if (exception is ConnectionDeniedException deniedException)
            {
                disconnectReason = deniedException.CloseReason;
                serverInitiated = true;
            }
            else if (exception is ConnectionTimeoutException)
            {
                disconnectReason = ConnectionCloseReason.Timeout;
            }
            else if (exception is ConnectionClosedException)
            {
                disconnectReason = ConnectionCloseReason.SocketClosedByPeer;
            }
            else
            {
                disconnectReason = ConnectionCloseReason.InvalidData;
            }

            Disconnect(disconnectReason, serverInitiated);
        }

        private void HandleDeliveryInfo(DeliveryInfo deliveryInfo)
        {
            outConnection.OnDeliveryInfo(deliveryInfo);
        }

        public void Disconnect()
        {
            Disconnect(ConnectionCloseReason.GracefulClose, false);
        }

        internal void Disconnect(ConnectionCloseReason reason, bool serverInitiated)
        {
            logger.Debug($"{nameof(Disconnect)}", ("state", ConnectionState), ("serverInitiated", serverInitiated));

            Reset();

            try
            {
                connection.Disconnect(reason, serverInitiated);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(Disconnect)),
                    ("exception", handlerException));
            }

            logger.Trace($"{nameof(Disconnect)} (end)", ("state", ConnectionState), ("serverInitiated", serverInitiated));
        }

        public bool IsConnected()
        {
            return ConnectionState == ConnectionState.Connected;
        }

        public bool IsConnecting()
        {
            return ConnectionState == ConnectionState.Connecting;
        }

        public bool IsDisconnected()
        {
            return ConnectionState == ConnectionState.Disconnected;
        }

        public void UpdateReceiving()
        {
            if (connection == null)
            {
                return;
            }

            ReceiveAndProcessPackets();
        }

        private void ReceiveAndProcessPackets()
        {
            receiveBuffer.Clear();
            connection.Receive(receiveBuffer);
            foreach (var packet in receiveBuffer)
            {
                try
                {
                    inConnection.ProcessIncomingPacket(packet.Stream);
                }
                catch (Exception e)
                {
                    logger.Error(Error.CoreClientHandlerException,
                        ("caller", nameof(InConnection.ProcessIncomingPacket)),
                        ("from", packet.From),
                        ("position", packet.Stream.Position),
                        ("header", $"Seq: {packet.SequenceId} Reliable: {packet.IsReliable} OOB: {packet.IsOob}"),
                        ("contents", Convert.ToBase64String(packet.Stream.GetBuffer())),
                        ("exception", e));

                    Disconnect(ConnectionCloseReason.InvalidData, false);
                    return;
                }
            }
        }

        public void UpdateSending()
        {
            if (connection == null)
            {
                return;
            }

            outConnection.Update(NetworkTime.ClientSimulationFrame);
            connection.Update();
        }

        public Entity CreateEntity(ICoherenceComponentData[] data, bool orphan)
        {
            var err = entityIdGenerator.GetEntity(out var id);
            if (err != EntityIDGenerator.Error.None)
            {
                throw new Exception($"out of entities");
            }

            logger.Trace("Create Entity", ("entity", id), ("comp", data.ToStringEx()));

            knownEntities.Add(id);
            authorityByEntity.Add(id, AuthorityType.Full);

            outConnection.CreateEntity(id, data);

            if (orphan)
            {
                SendAuthorityTransfer(id, ClientID.Server, true, AuthorityType.Full);
            }

            return id;
        }

        public bool IsEntityInAuthTransfer(Entity id)
        {
            return outConnection.IsEntityInAuthTransfer(id);
        }

        public bool CanSendUpdates(Entity id)
        {
            if (!HasAuthorityOverEntity(id, AuthorityType.State))
            {
                return false;
            }

            return outConnection.CanSendUpdates(id);
        }

        public void UpdateComponents(Entity id, ICoherenceComponentData[] data)
        {
            if (!HasAuthorityOverEntity(id, AuthorityType.State))
            {
                return;
            }

            if (data.Length < 1)
            {
                return;
            }

            outConnection.UpdateEntity(id, data);
        }

        public void RemoveComponents(Entity id, uint[] components)
        {
            if (!HasAuthorityOverEntity(id, AuthorityType.State))
            {
                return;
            }

            outConnection.RemoveComponent(id, components);
        }

        public void DestroyEntity(Entity id)
        {
            logger.Trace("Destroy Entity", ("entity", id));

            if (id == Entity.InvalidRelative)
            {
                throw new Exception("Trying to destroy an invalid entity.");
            }

            if (!EntityExists(id))
            {
                // This can be caused by a network destroy disabling and not destroying an entity
                // which causes it to then try to destroy the client entity but that has already
                // been destroyed.  Particularly, if the entity is a child of another entity that
                // was disabled because of duplicate destroy, this entity will also be disabled
                // but the client core doesn't know about this entity any more.  Nested prefabs
                // leads to all kinds of races and orders of operation.
                return;
            }

            var disconnected = ConnectionState == ConnectionState.Disconnected;
            var entityOwned = HasAuthorityOverEntity(id, AuthorityType.State);
            if (!entityOwned && !disconnected)
            {
                throw new Exception("Trying to destroy an entity without state authority.");
            }

            knownEntities.Remove(id);
            authorityByEntity.Remove(id);

            if (id.IsClientCreated() && !disconnected)
            {
                // It's a local entity ID, so we can recycle it.
                entityIdGenerator.ReleaseEntity(id);
            }

            outConnection.DestroyEntity(id);
        }

        public bool EntityExists(Entity entity)
        {
            return knownEntities.Contains(entity);
        }

        public bool HasAuthorityOverEntity(Entity entity, AuthorityType authorityType)
        {
            return authorityByEntity.TryGetValue(entity, out AuthorityType authType) && authType.Contains(authorityType);
        }

        public void SendCommand(IEntityCommand message, MessageTarget target, Entity id, ChannelID channelID)
        {
            if (!EntityExists(id))
            {
                return;
            }

            if (target == MessageTarget.AuthorityOnly && HasAuthorityOverEntity(id, AuthorityType.State))
            {
                return;
            }

            outConnection.PushCommand(message, target, id, channelID);
        }

        public void SendInput(IEntityInput message, long frame, Entity id)
        {
            var inputData = new InputData
            {
                Entity = id,
                Input = message,
                Frame = frame
            };
            outConnection.PushInput(inputData);
        }

        public void SendAuthorityRequest(Entity id, AuthorityType authorityType = AuthorityType.Full)
        {
            var req = protocolDefinition.CreateAuthorityRequest(id, ClientID, authorityType);
            SendCommand(req, MessageTarget.AuthorityOnly, id, ChannelID.Default);
        }

        public void SendAdoptOrphanRequest(Entity id)
        {
            var req = protocolDefinition.CreateAdoptOrphanCommand();
            SendCommand(req, MessageTarget.AuthorityOnly, id, ChannelID.Default);
        }

        public bool SendAuthorityTransfer(Entity id, ClientID newAuthority, bool authorized, AuthorityType transferredAuthorityType = AuthorityType.Full)
        {
            if (transferredAuthorityType == AuthorityType.None)
            {
                return false;
            }

            if (authorized)
            {
                if (!authorityByEntity.TryGetValue(id, out AuthorityType currentAuthorityType))
                {
                    logger.Warning(Warning.CoreClientAuthTransferNoAuthority,
                        ("entity", id),
                        ("transferredAuthorityType", transferredAuthorityType),
                        ("newAuthority", newAuthority));
                    return false;
                }

                if (!currentAuthorityType.CanTransfer(transferredAuthorityType))
                {
                    logger.Warning(Warning.CoreClientAuthInsufficient,
                        ("entity", id),
                        ("currentAuthorityType", currentAuthorityType),
                        ("transferredAuthorityType", transferredAuthorityType),
                        ("newAuthority", newAuthority));
                    return false;
                }

                if (transferredAuthorityType == AuthorityType.Input && newAuthority == ClientID.Server)
                {
                    logger.Warning(Warning.CoreClientAuthCantOrphanInput,
                        ("entity", id),
                        ("currentAuthorityType", currentAuthorityType),
                        ("transferredAuthorityType", transferredAuthorityType));
                    return false;
                }

                AuthorityType authorityTypeLeft = currentAuthorityType.Subtract(transferredAuthorityType);
                if (authorityTypeLeft == AuthorityType.None)
                {
                    authorityByEntity.Remove(id);
                }
                else
                {
                    authorityByEntity[id] = authorityTypeLeft;
                }

                if (transferredAuthorityType.Contains(AuthorityType.State))
                {
                    outConnection.HoldChangesForEntity(id);
                }

                if (authorityTypeLeft != currentAuthorityType)
                {
                    RaiseOnAuthorityChange(id, authorityTypeLeft);
                }
            }

            var req = protocolDefinition.CreateAuthorityTransfer(id, newAuthority, authorized, transferredAuthorityType);
            SendCommand(req, MessageTarget.Other, id, ChannelID.Default);

            return true;
        }

        public void SetFloatingOrigin(Vector3d newFloatingOrigin)
        {
            this.floatingOrigin = newFloatingOrigin;
            outConnection.SetFloatingOrigin(newFloatingOrigin);
            inConnection.SetFloatingOrigin(newFloatingOrigin);
        }

        public Vector3d GetFloatingOrigin()
        {
            return this.floatingOrigin;
        }

        public void SetTransportType(TransportType transportType, TransportConfiguration transportConfiguration)
        {
            this.transportFactory = new DefaultTransportFactory(transportType, transportConfiguration);
        }

        public void SetTransportFactory(ITransportFactory transportFactory)
        {
            this.transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
        }

        private void RaiseOnAuthorityTransferred(Entity id)
        {
            OnAuthorityTransferred?.Invoke(id);
        }

        private void OnEntityUpdates(List<IncomingEntityUpdate> updates)
        {
            foreach (var update in updates)
            {
                EntityWithMeta meta = update.Meta;
                bool known = EntityExists(meta.EntityId);

                logger.Trace("OnEntityUpdate",
                    ("entity", meta.EntityId.ToString()),
                    ("operation", meta.Operation.ToString()),
                    ("known", known));

                try
                {
                    switch (meta.Operation)
                    {
                        case EntityOperation.Create:
                            if (known)
                            {
                                HandleReceivedUpdate(update);
                                break;
                            }

                            HandleReceivedCreate(update);
                            break;

                        case EntityOperation.Update:
                            HandleReceivedUpdate(update);
                            break;

                        case EntityOperation.Destroy:
                            HandleReceivedDestroy(meta, known);
                            break;

                        default:
                            logger.Error(Error.CoreClientEntityUpdateUnknownOperation,
                                ("EntityID", meta.EntityId),
                                ("operation", meta.Operation));
                            break;
                    }
                }
                catch (Exception exception)
                {
                    logger.Error(Error.CoreClientEntityUpdateFailed,
                        ("EntityID", meta.EntityId),
                        ("operation", meta.Operation),
                        ("exception", exception));
                }
            }
        }

        private void HandleReceivedCreate(in IncomingEntityUpdate update)
        {
            EntityWithMeta meta = update.Meta;

            logger.Debug($"OnEntityUpdate New", ("EntityId", meta.EntityId), ("Comps", update));

            knownEntities.Add(meta.EntityId);
            AuthorityType authorityType = meta.Authority();
            if (authorityType != AuthorityType.None)
            {
                authorityByEntity.Add(meta.EntityId, authorityType);
                ackedEntities.Add(meta.EntityId);
            }

            try
            {
                OnEntityCreated?.Invoke(meta.EntityId, update);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnEntityCreated)),
                    ("exception", handlerException));
            }
        }

        private void HandleReceivedUpdate(in IncomingEntityUpdate update)
        {
            EntityWithMeta meta = update.Meta;

            logger.Trace($"OnEntityUpdate Update ", ("EntityId", meta.EntityId), ("Update", update));

            try
            {
                OnEntityUpdated?.Invoke(meta.EntityId, update);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnEntityUpdated)),
                    ("exception", handlerException));
            }

            bool changedOwnership = ProcessAuthorityChange(meta, out AuthorityType newAuthorityType);
            if (changedOwnership)
            {
                RaiseOnAuthorityChange(meta.EntityId, newAuthorityType);

                if (!meta.HasStateAuthority)
                {
                    outConnection.ClearAllChangesForEntity(meta.EntityId);
                }
            }
        }

        private void HandleReceivedDestroy(in EntityWithMeta meta, bool known)
        {
            logger.Debug("OnEntityUpdate destroy",
                ("EntityId", meta.EntityId),
                ("DestroyReason", meta.DestroyReason),
                ("known", known));

            if (!known)
            {
                return;
            }

            try
            {
                OnEntityDestroyed?.Invoke(meta.EntityId, meta.DestroyReason);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnEntityDestroyed)),
                    ("exception", handlerException));
            }

            if (!knownEntities.Contains(meta.EntityId))
            {
                // this probably already deleted itself from the core
                // by disabling because it was a duplicate.
                return;
            }

            knownEntities.Remove(meta.EntityId);
            authorityByEntity.Remove(meta.EntityId);

            if (meta.EntityId.IsClientCreated())
            {
                // It's a local entity ID, its authority was transferred.
                entityIdGenerator.ReleaseEntity(meta.EntityId);
            }
        }

        private bool ProcessAuthorityChange(EntityWithMeta meta, out AuthorityType newAuthorityType)
        {
            Entity entityId = meta.EntityId;
            newAuthorityType = meta.Authority();

            if (newAuthorityType == AuthorityType.None)
            {
                bool authorityChanged = authorityByEntity.Remove(entityId);
                if (authorityChanged)
                {
                    logger.Debug($"{nameof(ProcessAuthorityChange)} - authority lost",
                        ("entity", entityId));
                }

                return authorityChanged;
            }

            bool hasSomeAuthority = authorityByEntity.TryGetValue(entityId, out AuthorityType currentAuthority);
            if (hasSomeAuthority)
            {
                if (newAuthorityType != currentAuthority)
                {
                    logger.Debug($"{nameof(ProcessAuthorityChange)} - authority changed",
                        ("entity", entityId),
                        ("newAuthority", newAuthorityType),
                        ("currentAuthority", currentAuthority));

                    authorityByEntity[entityId] = newAuthorityType;
                    return true;
                }

                return false;
            }

            if (newAuthorityType != AuthorityType.None)
            {
                logger.Debug($"{nameof(ProcessAuthorityChange)} - authority gained",
                    ("entity", entityId),
                    ("newAuthority", newAuthorityType));

                authorityByEntity.Add(entityId, newAuthorityType);
                ackedEntities.Add(entityId);

                return true;
            }

            return false;
        }

        private void HandleCommand(IEntityCommand entityCommand, MessageTarget target, Entity id)
        {
            if (protocolDefinition.TryGetAuthorityRequestCommand(entityCommand, out ClientID requester, out AuthorityType authType))
            {
                if (!HasAuthorityOverEntity(id, AuthorityType.State))
                {
                    logger.Debug($"Received authority request command for non-owned entity",
                        ("EntityId", id),
                        ("Requester", requester),
                        ("AuthType", authType));
                    return;
                }

                RaiseOnAuthorityRequested(id, requester, authType);
            }
            else if (protocolDefinition.TryGetAuthorityTransferCommand(entityCommand, out _, out bool transferAccepted, out authType))
            {
                if (transferAccepted)
                {
                    //Intentionally throw away a command with the accepted auth since the RS will have already applied it.
                    return;
                }

                RaiseOnAuthorityRequestRejected(id, authType);
            }
            else if (protocolDefinition.TryGetSceneIndexChangedCommand(entityCommand, out int sceneIndex))
            {
                RaiseOnSceneIndexChanged(id, sceneIndex);
            }
            else
            {
                if (target == MessageTarget.AuthorityOnly && !HasAuthorityOverEntity(id, AuthorityType.State))
                {
                    logger.Warning(Warning.CoreClientCommandUnownedEntity,
                        ("target", target),
                        ("EntityId", id),
                        ("Command", entityCommand.GetType()));
                    return;
                }

                try
                {
                    logger.Trace($"INVOKE {id} - {entityCommand.GetComponentType()}");
                    OnCommand?.Invoke(entityCommand, target, id);
                }
                catch (Exception handlerException)
                {
                    logger.Error(Error.CoreClientHandlerException,
                        ("caller", nameof(OnCommand)),
                        ("exception", handlerException));
                }
            }
        }

        private void HandleInput(IEntityInput input, long frame, Entity entityId)
        {
            try
            {
                OnInput?.Invoke(input, frame, entityId);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnInput)),
                    ("exception", handlerException));
            }
        }

        public void Dispose()
        {
            Disconnect(ConnectionCloseReason.GracefulClose, false);
        }

        private bool UseDnsResolution()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }

        private void RaiseOnAuthorityRequested(Entity id, ClientID requester, AuthorityType authType)
        {
            try
            {
                OnAuthorityRequested?.Invoke(new AuthorityRequest(id, requester, authType));
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnAuthorityRequested)),
                    ("exception", handlerException));
            }
        }

        private void RaiseOnAuthorityRequestRejected(Entity id, AuthorityType authType)
        {
            try
            {
                OnAuthorityRequestRejected?.Invoke(new AuthorityRequestRejection(id, authType));
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnAuthorityRequestRejected)),
                    ("exception", handlerException));
            }
        }

        private void RaiseOnAuthorityChange(Entity id, AuthorityType authorityType)
        {
            try
            {
                OnAuthorityChange?.Invoke(new AuthorityChange(id, authorityType));
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnAuthorityChange)),
                    ("exception", handlerException));
            }
        }

        private void RaiseOnConnectedEndpoint()
        {
            try
            {
                OnConnectedEndpoint?.Invoke(lastConnectData);
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnConnectedEndpoint)),
                    ("exception", handlerException));
            }
        }

        private void RaiseOnSceneIndexChanged(Entity id, int sceneIndex)
        {
            try
            {
                OnSceneIndexChanged?.Invoke(new SceneIndexChanged(id, sceneIndex));
            }
            catch (Exception handlerException)
            {
                logger.Error(Error.CoreClientHandlerException,
                    ("caller", nameof(OnSceneIndexChanged)),
                    ("exception", handlerException));
            }
        }

        // For debug testing only.
        public string DebugGetTransportDescription() => connection.TransportDescription;

        public void DebugHoldAllPackets(bool drop)
        {
            networkConditionerConfig.HoldOutgoingPackets = drop;
        }

        public void DebugReleaseAllHeldPackets()
        {
            networkConditioner.ReleaseAllHeldOutgoingPackets();
        }

        public void DebugSetNetworkCondition(Condition condition)
        {
            networkConditionerConfig.Conditions = condition;
        }

        public void DebugStopSerializingUpdates(bool stop)
        {
            networkConditionerConfig.CanSend = !stop;
        }

        public void DebugDropNextOutPacket(Action callback)
        {
            networkConditionerConfig.DropNextOutPacket = true;
            networkConditionerConfig.OnNextOutPacketDropped = callback;
        }

        public void DebugOnNextPacketSentOneShot(Action callback)
        {
            networkConditionerConfig.OnNextPacketSentOneShot += callback;
        }

        private void OnServerSimulationFrameReceived(AbsoluteSimulationFrame serverFrame)
        {
            networkTime.SetServerSimulationFrame(serverFrame, connection.Ping);
        }
    }
}
