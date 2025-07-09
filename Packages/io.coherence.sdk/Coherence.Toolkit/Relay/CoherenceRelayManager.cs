namespace Coherence.Toolkit.Relay
{
    using Connection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Logger = Log.Logger;

    /// <summary>
    ///     Manages <see cref="IRelay" /> and <see cref="IRelayConnection" />s when using custom
    ///     relayed transport, e.g. Steam.
    /// </summary>
    public class CoherenceRelayManager
    {
        private readonly Dictionary<IRelayConnection, RelayConnectionHolder> connections =
            new ();

        private readonly List<IRelayConnection> pendingRemoves = new ();

        private EndpointData endpointData;
        private IRelay relay;
        private bool isOpen;
        private bool isUpdatingConnections;

        private readonly Logger logger;

        internal delegate RelayConnectionHolder RelayConnectionHolderCreator(IRelayConnection connection);
        private readonly RelayConnectionHolderCreator relayConnectionHolderCreator = null;

        internal CoherenceRelayManager(Logger logger)
        {
            this.logger = logger.With<CoherenceRelayManager>();
        }

        internal CoherenceRelayManager(RelayConnectionHolderCreator relayConnectionHolderCreator, Logger logger)
        {
            this.logger = logger.With<CoherenceRelayManager>();
            this.relayConnectionHolderCreator = relayConnectionHolderCreator;
        }

        /// <remarks>Called by CoherenceBridge.</remarks>
        internal void Open(EndpointData newEndpointData)
        {
            if (relay == null)
            {
                return;
            }

            endpointData = newEndpointData;
            isOpen = true;

            try
            {
                relay.Open();
            }
            catch (Exception e)
            {
                logger.Error(Log.Error.ToolkitRelayOpenException, e.ToString());

                // Disable the relay if Open() threw an exception
                isOpen = false;
            }
        }

        /// <remarks>Called by CoherenceBridge.</remarks>
        internal void Close()
        {
            if (relay == null)
            {
                return;
            }

            isOpen = false;

            // ToList() prevents InvalidOperationException
            foreach (var connection in connections.Keys.ToList())
            {
                CloseAndRemoveRelayConnection(connection);
            }

            try
            {
                relay.Close();
            }
            catch (Exception e)
            {
                logger.Error(Log.Error.ToolkitRelayCloseException, e.ToString());
            }

            relay = null;
        }

        /// <remarks>Called by CoherenceBridge.</remarks>
        internal void Update()
        {
            if (!isOpen)
            {
                return;
            }

            if (relay == null)
            {
                return;
            }

            try
            {
                relay.Update();
            }
            catch (Exception e)
            {
                logger.Error(Log.Error.ToolkitRelayUpdateException, e.ToString());
            }

            UpdateRelayedConnections();
            HandlePendingConnectionRemovals();
        }

        private void UpdateRelayedConnections()
        {
            isUpdatingConnections = true;
            {
                foreach (var connection in connections.Values)
                {
                    connection.Update();
                }
            }
            isUpdatingConnections = false;
        }

        private void HandlePendingConnectionRemovals()
        {
            foreach (var removedConnection in pendingRemoves)
            {
                connections.Remove(removedConnection);
            }

            pendingRemoves.Clear();
        }

        /// <summary>
        ///     Registers a new <see cref="IRelayConnection" />. Should be called by the implementation of
        ///     <see cref="IRelay" /> whenever a new client connection is established.
        /// </summary>
        /// <param name="connection">New connection to be relayed to the Replication Server.</param>
        public void OpenRelayConnection(IRelayConnection connection)
        {
            var connectionHolder = this.relayConnectionHolderCreator != null
                ? this.relayConnectionHolderCreator(connection)
                : new RelayConnectionHolder(endpointData, connection, logger);

            connectionHolder.OnError += HandleConnectionError;
            connections.Add(connection, connectionHolder);
        }

        /// <summary>
        ///     De-registers a <see cref="IRelayConnection" />. Should be called by the implementation of
        ///     <see cref="IRelay" /> whenever a client connection is dropped.
        /// </summary>
        /// <param name="connection">A connection to stop relaying to the Replication Server.</param>
        public void CloseAndRemoveRelayConnection(IRelayConnection connection)
        {
            if (relay == null)
            {
                // Relay has already shutdown
                return;
            }

            if (!connections.TryGetValue(connection, out var connectionHolder))
            {
                logger.Error(Log.Error.ToolkitRelayRemoveNotFound, ("connection", connection));
                return;
            }

            connectionHolder.OnError -= HandleConnectionError;
            connectionHolder.Close();

            if (isUpdatingConnections)
            {
                // Connections can't be removed immediately when updating
                // as that would modify the dictionary being iterated over.
                pendingRemoves.Add(connection);
            }
            else
            {
                connections.Remove(connection);
            }
        }

        /// <remarks>
        ///     Called from the <see cref="RelayConnectionHolder" /> if there is an error in the
        ///     connection to the Replication Server.
        /// </remarks>
        private void HandleConnectionError(IRelayConnection connection, ConnectionException e)
        {
            logger.Error(Log.Error.ToolkitRelayConnection, ("exception", e));

            CloseAndRemoveRelayConnection(connection);
        }

        /// <remarks>
        ///     Called from <see cref="CoherenceBridge.SetRelay" />.
        /// </remarks>
        internal void SetRelay(IRelay newRelay)
        {
            Close();

            relay = newRelay;

            if (relay != null)
            {
                relay.RelayManager = this;
            }
        }
    }
}
