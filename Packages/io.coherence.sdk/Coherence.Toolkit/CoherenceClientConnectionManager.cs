// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Connection;
    using Entities;
    using Log;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Component = UnityEngine.Component;
    using Logger = Log.Logger;

    public delegate CoherenceSync ClientConnectionPrefabProvider(ClientID clientId, ConnectionType connectionType);

    /// <summary>
    /// Eases access to client connections.
    /// Used by <see cref="CoherenceBridge"/> and <see cref="CoherenceSceneManager"/>.
    /// </summary>
    public class CoherenceClientConnectionManager : IClientConnectionManager
    {
        /// <summary>
        /// Called before creating the <see cref="CoherenceClientConnection" />.
        /// Allows for providing custom prefabs to be used as <see cref="CoherenceClientConnection" />.
        /// Takes precedence over the <see cref="CoherenceBridge.ClientConnectionEntry" />.
        /// If no provider was subscribed, or if provider returns <see langword="null"/>,
        /// <see cref="CoherenceBridge.ClientConnectionEntry" /> is used.
        /// </summary>
        public event ClientConnectionPrefabProvider ProvidePrefab
        {
            add
            {
                if (!bridge.EnableClientConnections)
                {
                    PrintConnectionWithoutQueryError($"The {nameof(ProvidePrefab)} callback has been registered");
                }

                providePrefab += value;
            }
            remove => providePrefab -= value;
        }
        private event ClientConnectionPrefabProvider providePrefab;

        /// <summary>
        /// Called when a new <see cref="CoherenceClientConnection" /> is synced, including the local connection.
        /// </summary>
        public event Action<CoherenceClientConnection> OnCreated;

        /// <summary>
        /// Called when an existing <see cref="CoherenceClientConnection" /> is destroyed, including the local connection.
        /// </summary>
        public event Action<CoherenceClientConnection> OnDestroyed;

        /// <summary>
        /// Called when all connections present at the moment of joining the session get synced.
        /// </summary>
        public event Action<CoherenceClientConnectionManager> OnSynced;

        /// <summary>
        /// Returns the number of active client connections, including the local connection.
        /// </summary>
        public int ClientConnectionCount => connectionsByEntityId.Count;

        private CoherenceClientConnection myClientConnection;
        private CoherenceClientConnection simulatorConnection;
        private readonly ICoherenceBridge bridge;
        private readonly Logger logger;

        private readonly Dictionary<Entity, CoherenceClientConnection> connectionsByEntityId = new Dictionary<Entity, CoherenceClientConnection>();
        private readonly Dictionary<ClientID, Entity> entityIdByClientId = new Dictionary<ClientID, Entity>();

        internal CoherenceClientConnectionManager(ICoherenceBridge bridge, Logger logger)
        {
            this.bridge = bridge;
            this.logger = logger;

            if (bridge.GetClientConnectionEntry() != null && !bridge.EnableClientConnections)
            {
                PrintConnectionWithoutQueryError($"{nameof(bridge.GetClientConnectionEntry)} has been provided");
            }
        }

        internal void PrintConnectionWithoutQueryError(string cause)
        {
            logger.Warning(Warning.ToolkitClientConnectionManagerQueryError,
                $"{cause}, however the 'Enable Client Connections' toggle is turned off. " +
                    $"To turn it on, tick the 'Enable Client Connections' toggle in the " +
                    $"{nameof(CoherenceBridge)} inspector. Or set the " +
                    $"{nameof(CoherenceBridge)}.{nameof(CoherenceBridge.EnableClientConnections)} " +
                    $"property to 'true'.",
                ("bridge", bridge));
        }

        /// <summary>
        /// Gets a client connection prefab, as set on the associated <see cref="CoherenceBridge"/>.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="connectionType">The connection type associated with that prefab.</param>
        /// <param name="onLoaded">A callback with the resulting <see cref="CoherenceSync"/>.</param>
        /// <seealso cref="CoherenceBridge"/>
        public void GetPrefab(ClientID clientId, ConnectionType connectionType, Action<ICoherenceSync> onLoaded)
        {
            var userProvidedPrefab = providePrefab?.Invoke(clientId, connectionType);
            if (userProvidedPrefab != null)
            {
                onLoaded.Invoke(userProvidedPrefab);
                return;
            }

            var connectionEntry = connectionType == ConnectionType.Client
                ? bridge.GetClientConnectionEntry()
                : bridge.GetSimulatorConnectionEntry();

            if (connectionEntry != null)
            {
                connectionEntry.Provider.LoadAsset(connectionEntry.ID, onLoaded);
                return;
            }

            onLoaded.Invoke(null);
        }

        internal Action<CoherenceClientConnection> OnMyClientConnection;

        // TODO internalize
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Add(CoherenceClientConnection connection)
        {
            try
            {
                connectionsByEntityId.Add(connection.EntityId, connection);
            }
            catch (ArgumentException)
            {
                logger.Error(Error.ToolkitClientConnectionManagerFailedToAddConnection,
                    ("error", "EntityID already exists"),
                    ("entity", connection.EntityId),
                    ("clientID", connection.ClientId));
                return;
            }

            try
            {
                entityIdByClientId.Add(connection.ClientId, connection.EntityId);
            }
            catch (ArgumentException)
            {
                connectionsByEntityId.Remove(connection.EntityId);

                logger.Error(Error.ToolkitClientConnectionManagerFailedToAddConnection,
                    ("error", "ClientID already exists"),
                    ("entity", connection.EntityId),
                    ("clientID", connection.ClientId));

                return;
            }

            if (connection.IsMyConnection)
            {
                myClientConnection = connection;
                OnMyClientConnection?.Invoke(myClientConnection);
            }

            if (connection.Type == ConnectionType.Simulator)
            {
                simulatorConnection = connection;
            }

            logger.Debug($"Added connection", ("entity", connection.EntityId),
                ("clientID", connection.ClientId), ("type", connection.Type), ("isMine", connection.IsMyConnection));

            OnCreated?.Invoke(connection);
        }

        // TODO internalize
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Remove(Entity entityID)
        {
            if (!connectionsByEntityId.TryGetValue(entityID, out var connection))
            {
                return false;
            }

            if (connection == myClientConnection)
            {
                myClientConnection = null;
            }

            if (connection == simulatorConnection)
            {
                simulatorConnection = null;
            }

            connectionsByEntityId.Remove(entityID);

            if (!entityIdByClientId.Remove(connection.ClientId))
            {
                logger.Error(Error.ToolkitClientConnectionManagerFailedToRemoveConnection,
                    ("entity", connection.EntityId),
                    ("clientID", connection.ClientId));
            }

            logger.Debug($"Removed connection", ("entity", connection.EntityId),
                ("clientID", connection.ClientId), ("type", connection.Type), ("isMine", connection.IsMyConnection));

            OnDestroyed?.Invoke(connection);
            return true;

        }

        internal void CleanUp()
        {
            var myConnection = myClientConnection;

            myClientConnection = null;
            simulatorConnection = null;

            connectionsByEntityId.Clear();
            entityIdByClientId.Clear();

            if (myConnection != null)
            {
                var sync = myConnection.Sync;
                if (sync)
                {
                    CoroutineRunner.StartCoroutine(DestroyNextFrame(sync));
                }

                OnDestroyed?.Invoke(myConnection);
            }

            static IEnumerator DestroyNextFrame(CoherenceSync sync)
            {
                yield return null;

                if (sync)
                {
                    sync.CoherenceSyncConfig.Instantiator.Destroy(sync);
                }
            }
        }

        internal void HandleGlobalQuerySynced()
        {
            if (!bridge.EnableClientConnections)
            {
                return;
            }

            OnSynced?.Invoke(this);
        }

        /// <summary>
        /// Client connection associated with the current client.
        /// </summary>
        /// <returns>
        /// Client connection, or <see langword="null"/> if client connections are disabled.
        /// </returns>
        /// <seealso cref="CoherenceBridge.EnableClientConnections"/>
        public CoherenceClientConnection GetMine()
        {
            return bridge.EnableClientConnections ? myClientConnection : null;
        }

        /// <summary>
        /// Client connection associated with a simulator.
        /// </summary>
        /// <returns>
        /// Client connection, or <see langword="null"/> if client connections are disabled.
        /// </returns>
        /// <seealso cref="CoherenceBridge.EnableClientConnections"/>
        public CoherenceClientConnection GetSimulator()
        {
            return bridge.EnableClientConnections ? simulatorConnection : null;
        }

        /// <summary>
        /// Client connection associated with a given <paramref name="clientId" />
        /// </summary>
        /// <returns>
        /// Client connection, or <see langword="null"/> if the entity isn't found.
        /// </returns>
        public CoherenceClientConnection Get(ClientID clientId)
        {
            return !entityIdByClientId.TryGetValue(clientId, out Entity entityId)
                ? null
                : Get(entityId);
        }

        /// <summary>
        /// Client connection associated with a given <paramref name="entityId" />
        /// </summary>
        /// <returns>
        /// Client connection, or <see langword="null"/> if the entity isn't found.
        /// </returns>
        public CoherenceClientConnection Get(Entity entityId)
        {
            return connectionsByEntityId.TryGetValue(entityId, out CoherenceClientConnection connection)
                ? connection
                : null;
        }

        /// <summary>Collection of all client connections (both simulator and regular clients), including the local connection.</summary>
        public IEnumerable<CoherenceClientConnection> GetAll()
        {
            return connectionsByEntityId.Values;
        }

        /// <summary>Collection of all client connections (with no simulators), including the local connection.</summary>
        public IEnumerable<CoherenceClientConnection> GetAllClients()
        {
            using var connections = connectionsByEntityId.Values.GetEnumerator();

            while (connections.MoveNext())
            {
                if (connections.Current?.Type == ConnectionType.Client)
                {
                    yield return connections.Current;
                }
            }
        }

        /// <summary>Collection of all simulator connections, including the local connection.</summary>
        public IEnumerable<CoherenceClientConnection> GetAllSimulators()
        {
            using var connections = connectionsByEntityId.Values.GetEnumerator();

            while (connections.MoveNext())
            {
                if (connections.Current?.Type == ConnectionType.Simulator)
                {
                    yield return connections.Current;
                }
            }
        }

        /// <summary>
        /// Collection of all client connections (both simulator and clients), except for the local one.
        /// </summary>
        public IEnumerable<CoherenceClientConnection> GetOther()
        {
            using var connections = connectionsByEntityId.Values.GetEnumerator();

            while (connections.MoveNext())
            {
                if (connections.Current != myClientConnection)
                {
                    yield return connections.Current;
                }
            }
        }


        /// <summary>
        /// Collection of all client connections except for the local one.
        /// </summary>
        public IEnumerable<CoherenceClientConnection> GetOtherClients()
        {
            using var connections = connectionsByEntityId.Values.GetEnumerator();

            while (connections.MoveNext())
            {
                if (connections.Current != myClientConnection && connections.Current?.Type == ConnectionType.Client)
                {
                    yield return connections.Current;
                }
            }
        }

        /// <summary>
        /// Returns a collection of all simulator connections, except for the local one.
        /// </summary>
        public IEnumerable<CoherenceClientConnection> GetOtherSimulators()
        {
            using var connections = connectionsByEntityId.Values.GetEnumerator();

            while (connections.MoveNext())
            {
                if (connections.Current != myClientConnection && connections.Current?.Type == ConnectionType.Simulator)
                {
                    yield return connections.Current;
                }
            }
        }

        /// <inheritdoc cref="SendMessage(System.Type,string,Coherence.Entities.Entity,Coherence.MessageTarget,object[])"/>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        public bool SendMessage<TTarget>(string methodName, Entity entityId, MessageTarget target,
            params object[] args) where TTarget : Component
        {
            return SendMessage(typeof(TTarget), methodName, entityId, target, args);
        }

        /// <summary>
        /// Sends a client message to the connection whose <see cref="CoherenceClientConnection.EntityId" /> matches the
        /// <paramref name="entityId" />.
        /// </summary>
        /// <returns>
        /// <see langword="false" /> if connection for a given <paramref name="entityId" /> was not found, has no associated
        /// <see cref="CoherenceSync" />, or if sending has failed.
        /// </returns>
        public bool SendMessage(Type targetType, string methodName, Entity entityId, MessageTarget target,
            params object[] args)
        {
            CoherenceClientConnection targetConnection = myClientConnection?.EntityId == entityId
                ? myClientConnection
                : Get(entityId);
            if (targetConnection == null)
            {
                return false;
            }

            if (!bridge.EnableClientConnections)
                PrintConnectionWithoutQueryError("Trying to call SendMessage on ClientConnection");

            return targetConnection.Sync != null &&
                   targetConnection.Sync.SendCommand(targetType, methodName, target, args);
        }

        /// <inheritdoc cref="SendMessage(System.Type,string,Coherence.Entities.Entity,Coherence.MessageTarget,object[])"/>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        public bool SendMessage<TTarget>(string methodName, ClientID clientId, MessageTarget target,
            params object[] args) where TTarget : Component
        {
            if (!entityIdByClientId.TryGetValue(clientId, out Entity entityId))
            {
                return false;
            }

            return SendMessage<TTarget>(methodName, entityId, target, args);
        }

        /// <summary>
        /// Sends a client message to the connection whose <see cref="CoherenceClientConnection.ClientId" /> matches the <paramref name="clientId" />.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if connection for a given <paramref name="clientId" /> was not found,
        /// has no associated <see cref="CoherenceSync" />, or if sending has failed.
        /// </returns>
        public bool SendMessage(Type targetType, string methodName, ClientID clientId, MessageTarget target,
            params object[] args)
        {
            if (!entityIdByClientId.TryGetValue(clientId, out Entity entityId))
            {
                return false;
            }

            return SendMessage(targetType, methodName, entityId, target, args);
        }

        /// <inheritdoc cref="SendMessageToAll"/>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        public bool SendMessageToAll<TTarget>(string methodName, MessageTarget target, bool sendToSelf,
            params object[] args) where TTarget : Component
        {
            bool result = true;

            var enumerator = sendToSelf ? GetAll() : GetOther();

            foreach (var connection in enumerator)
            {
                result &= SendMessage<TTarget>(methodName, connection.ClientId, target, args);
            }

            return result;
        }

        /// <summary>
        /// Sends a client message to every connection (both clients and simulators).
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if sending has failed for any of the clients.
        /// </returns>
        public bool SendMessageToAll(Type targetType, string methodName, MessageTarget target, bool sendToSelf,
            params object[] args)
        {
            bool result = true;

            var enumerator = sendToSelf ? GetAll() : GetOther();

            foreach (var connection in enumerator)
            {
                result &= SendMessage(targetType, methodName, connection.ClientId, target, args);
            }

            return result;
        }

        /// <inheritdoc cref="SendMessageToAllClients"/>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        public bool SendMessageToAllClients<TTarget>(string methodName, MessageTarget target, bool sendToSelf,
            params object[] args) where TTarget : Component
        {
            bool result = true;

            var enumerator = sendToSelf ? GetAllClients() : GetOtherClients();

            foreach (var connection in enumerator)
            {
                result &= SendMessage<TTarget>(methodName, connection.ClientId, target, args);
            }

            return result;
        }

        /// <summary>
        /// Sends a client message to every client connection.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if sending has failed for any of the clients.
        /// </returns>
        public bool SendMessageToAllClients(Type targetType, string methodName, MessageTarget target, bool sendToSelf,
            params object[] args)
        {
            bool result = true;

            var enumerator = sendToSelf ? GetAllClients() : GetOtherClients();

            foreach (var connection in enumerator)
            {
                result &= SendMessage(targetType, methodName, connection.ClientId, target, args);
            }

            return result;
        }

        /// <inheritdoc cref="SendMessageToAllSimulators"/>
        /// <typeparam name="TTarget">Type of the component that should receive this message.</typeparam>
        public bool SendMessageToAllSimulators<TTarget>(string methodName, MessageTarget target, bool sendToSelf,
            params object[] args) where TTarget : Component
        {
            bool result = true;

            var enumerator = sendToSelf ? GetAllSimulators() : GetOtherSimulators();

            foreach (var connection in enumerator)
            {
                result &= SendMessage<TTarget>(methodName, connection.ClientId, target, args);
            }

            return result;
        }

        /// <summary>
        /// Sends a client message to every simulator connection.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if sending has failed for any of the clients.
        /// </returns>
        public bool SendMessageToAllSimulators(Type targetType, string methodName, MessageTarget target, bool sendToSelf,
            params object[] args)
        {
            bool result = true;

            var enumerator = sendToSelf ? GetAllSimulators() : GetOtherSimulators();

            foreach (var connection in enumerator)
            {
                result &= SendMessage(targetType, methodName, connection.ClientId, target, args);
            }

            return result;
        }
    }
}
