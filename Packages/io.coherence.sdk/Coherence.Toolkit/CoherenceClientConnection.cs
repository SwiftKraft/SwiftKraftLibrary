// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Connection;
    using Entities;
    using Log;
    using Logger = Log.Logger;
    using System;
    using UnityEngine;

    /// <summary>
    /// This class is a base of a connection entity, used to mark client presence and to send messages between clients.
    /// </summary>
    /// <remarks>
    /// To use the connection system:
    /// Create a <see cref="CoherenceSync"/> prefab.
    /// Assign the prefab on <see cref="CoherenceBridge.ClientConnectionEntry"/> or <see cref="CoherenceBridge.SimulatorConnectionEntry"/>.
    /// Optionally, use <see cref="CoherenceBridge.ClientConnections" />'s
    /// <see cref="CoherenceClientConnectionManager.ProvidePrefab" /> event for fine-grained control over entities
    /// instantiated for each connection.
    /// Make sure that <see cref="CoherenceBridge.EnableClientConnections" /> is enabled.
    ///
    /// The prefab will be instantiated for every new client connection. Since connection entities use a
    /// global <see cref="CoherenceLiveQuery"/>, they will be visible regardless of query filtering.
    /// </remarks>
    /// <seealso cref="CoherenceBridge.ClientConnections"/>
    public class CoherenceClientConnection
    {
        private NetworkEntityState networkEntity;
        private ICoherenceBridge bridge;
        private readonly Entity entityId;
        private Logger logger;

        /// <summary>
        /// <see langword="true"/> if this <see cref="CoherenceClientConnection" /> belongs to the local client.
        /// </summary>
        public bool IsMyConnection { get; }

        /// <summary>
        /// Unique ID that identifies the client, assigned by the replication server.
        /// </summary>
        public ClientID ClientId { get; }

        /// <summary>
        /// Connection type against the replication server.
        /// </summary>
        public ConnectionType Type { get; }

        /// <summary>
        /// <see cref="UnityEngine.GameObject"/> associated with the client connection.
        /// </summary>
        public GameObject GameObject
        {
            get
            {
                var sync = networkEntity?.Sync;

                if (sync != null && sync is MonoBehaviour mb)
                {
                    return mb.gameObject;
                }

                return null;
            }
        }

        /// <summary>
        /// Client connection entity.
        /// </summary>
        public Entity EntityId => entityId;

        /// <summary>
        /// <see cref="CoherenceBridge"/> associated with the client connection.
        /// </summary>
        public ICoherenceBridge CoherenceBridge
        {
            get
            {
                PrintConditionalWarning(nameof(bridge));
                return bridge;
            }
        }

        /// <summary>
        /// <see cref="CoherenceSync"/> associated with the client connection.
        /// </summary>
        public CoherenceSync Sync
        {
            get
            {
                PrintConditionalWarning(nameof(Sync));
                return networkEntity?.Sync as CoherenceSync;
            }
        }

        /// <summary>
        /// Entity state associated with the client connection.
        /// </summary>
        public NetworkEntityState NetworkEntity => networkEntity;

        internal CoherenceClientConnection(ICoherenceBridge bridge, Entity entityId,
            ClientID clientId, ConnectionType type, bool isMine)
        {
            this.bridge = bridge;
            this.entityId = entityId;
            this.ClientId = clientId;
            this.Type = type;
            this.IsMyConnection = isMine;
            this.logger = Log.GetLogger<CoherenceClientConnection>();
        }

        internal CoherenceClientConnection(ICoherenceBridge bridge, NetworkEntityState networkEntity,
            Entity entityId, ClientID clientId, ConnectionType type)
            : this(bridge, entityId, clientId, type, networkEntity.AuthorityType.Value.ControlsState())
        {
            this.networkEntity = networkEntity;
            this.logger = Log.GetLogger<CoherenceClientConnection>();
        }

        private void PrintConditionalWarning(string prop)
        {
            if (!bridge.EnableClientConnections)
            {
                bridge.ClientConnections.PrintConnectionWithoutQueryError($"Trying to get {prop} property from CoherenceClientConnection");
            }
        }

        /// <summary>Sends a message to this ClientConnection.</summary>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        /// <returns><see langword="false"/> if connection has no associated <see cref="CoherenceSync" />, or if sending has failed.</returns>
        public bool SendClientMessage<TTarget>(string methodName, MessageTarget target, params object[] args)
            where TTarget : Component
        {
            return bridge.ClientConnections.SendMessage<TTarget>(methodName, entityId, target, args);
        }

        /// <summary>Sends a message to this ClientConnection.</summary>
        /// <returns><see langword="false"/> if connection has no associated <see cref="CoherenceSync" />, or if sending has failed.</returns>
        public bool SendClientMessage(Type targetType, string methodName, MessageTarget target, params object[] args)
        {
            return bridge.ClientConnections.SendMessage(targetType, methodName, entityId, target, args);
        }

        /// <inheritdoc cref="CoherenceClientConnectionManager.SendMessage{TTarget}(string,Coherence.Entities.Entity,Coherence.MessageTarget,object[])"/>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        public bool SendClientMessage<TTarget>(string methodName, Entity entityID, MessageTarget target,
            params object[] args) where TTarget : Component
        {
            return bridge.ClientConnections.SendMessage<TTarget>(methodName, entityID, target, args);
        }

        /// <inheritdoc cref="CoherenceClientConnectionManager.SendMessage(System.Type,string,Coherence.Entities.Entity,Coherence.MessageTarget,object[])"/>
        public bool SendClientMessage(Type targetType, string methodName, Entity entityID, MessageTarget target,
            params object[] args)
        {
            return bridge.ClientConnections.SendMessage(targetType, methodName, entityID, target, args);
        }

        /// <inheritdoc cref="CoherenceClientConnectionManager.SendMessage{TTarget}(string,ClientID,Coherence.MessageTarget,object[])"/>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        public bool SendClientMessage<TTarget>(string methodName, ClientID clientID, MessageTarget target,
            params object[] args) where TTarget : Component
        {
            return bridge.ClientConnections.SendMessage<TTarget>(methodName, clientID, target, args);
        }

        /// <inheritdoc cref="CoherenceClientConnectionManager.SendMessage(System.Type,string,ClientID,Coherence.MessageTarget,object[])"/>
        public bool SendClientMessage(Type targetType, string methodName, ClientID clientID, MessageTarget target,
            params object[] args)
        {
            return bridge.ClientConnections.SendMessage(targetType, methodName, clientID, target, args);
        }

        /// <summary>
        /// The connection scene is used for filtering out updates on the replication server.
        /// </summary>
        /// <remarks>
        /// Can be used for keeping track of which scene the player is in.
        /// Note that while this piece of data is technically synced to other clients, we currently don't track it on those client connections.
        /// </remarks>
        /// <param name="newSceneIndex">The new scene index that the client should use.</param>
        internal void SendConnectionSceneUpdate(uint newSceneIndex)
        {
            if (!IsMyConnection)
            {
                logger.Warning(Warning.ToolkitClientConnectionSceneChangeWrongOwner,
                    $"Can't send connection scene update on {nameof(CoherenceClientConnection)} that belongs to someone else.");

                return;
            }

            var update = Impl.CreateConnectionSceneUpdateInternal(newSceneIndex, bridge.NetworkTime.ClientSimulationFrame);
            bridge.Client.UpdateComponents(entityId, new ICoherenceComponentData[] { update });
        }
    }
}
