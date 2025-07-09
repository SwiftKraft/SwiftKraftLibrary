// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Connection;
    using Entities;
    using Log;
    using System;
    using UnityEngine;
    using Logger = Log.Logger;

    /// <summary>
    /// Handles authority-related operations on the client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by <see cref="CoherenceBridge"/>.
    /// </para>
    /// <para>
    /// Authority is a core property of an entity. The replication server reads the data coming from entities
    /// with authority and propagates it through the network, to entities replicated that do not have authority.
    /// </para>
    /// <para>
    /// Entities without authority will get loaded with the data that comes from the entities that do have authority.
    /// </para>
    /// </remarks>
    public sealed class AuthorityManager
    {
        private IClient client;
        private CoherenceBridge bridge;

        private Logger logger;

        internal AuthorityManager(IClient client, CoherenceBridge bridge)
        {
            logger = bridge.Logger != null
                ? bridge.Logger.With<AuthorityManager>()
                : Log.GetLogger<AuthorityManager>();

            this.client = client;
            this.bridge = bridge;

            client.OnAuthorityRequested += HandleAuthorityRequest;
            client.OnAuthorityRequestRejected += HandleAuthorityRequestRejected;
            client.OnAuthorityChange += HandleAuthorityChanged;
            client.OnAuthorityTransferred += HandleAuthorityTransferred;
        }

        /// <summary>
        /// Try to get authority over this entity.
        /// </summary>
        /// <remarks>
        /// Even if the request operation succeeds, the authority request can be rejected.
        /// </remarks>
        /// <param name="state">State of the entity to request authority on.</param>
        /// <param name="authorityType">The kind of authority to request for.</param>
        /// <returns>
        /// <see langword="true"/> request succeeds. <see langword="false"/> otherwise.
        /// </returns>
        /// <seealso cref="CoherenceSync.OnStateAuthority"/>
        /// <seealso cref="CoherenceSync.OnStateRemote"/>
        /// <seealso cref="CoherenceSync.OnInputAuthority"/>
        /// <seealso cref="CoherenceSync.OnInputRemote"/>
        /// <seealso cref="CoherenceSync.OnAuthorityRequestRejected"/>
        /// <seealso cref="CoherenceSync.OnAuthorityRequested"/>
        public bool RequestAuthority(NetworkEntityState state, AuthorityType authorityType)
        {
            if (!AssertNetworkEntityState(state))
            {
                return false;
            }

            if (authorityType == AuthorityType.None)
            {
                logger.Warning(Warning.ToolkitAuthorityManagerRequest,
                    $"entityID: {state.EntityID} cannot request authority of type {nameof(AuthorityType.None)}.");
                return false;
            }

            if (state.Sync.SimulationTypeConfig != CoherenceSync.SimulationType.ServerSideWithClientInput &&
                state.Sync.AuthorityTransferTypeConfig == CoherenceSync.AuthorityTransferType.NotTransferable)
            {
                logger.Warning(Warning.ToolkitAuthorityManagerRequest,
                    $"entityID: {state.EntityID} is NotTransferable, this authority request will be cancelled.");
                return false;
            }

            if (state.AuthorityType.Value == authorityType)
            {
                logger.Warning(Warning.ToolkitAuthorityManagerRequest,
                    $"entityID: {state.EntityID} already has requested authority type ({authorityType}), this authority request will be cancelled.");
                return false;
            }

            if (state.IsOrphaned)
            {
                logger.Warning(Warning.ToolkitAuthorityManagerRequest,
                    $"entityID: {state.EntityID} cannot request authority over an orphaned entity. Please use Adopt instead.");
                return false;
            }

            client.SendAuthorityRequest(state.EntityID, authorityType);

            return true;
        }

        /// <summary>
        /// Give away authority over this entity to another client.
        /// </summary>
        /// <remarks>
        /// Requires the client to have authority.
        /// Even if the transfer operation can be started, the transfer itself can be rejected.
        /// </remarks>
        /// <param name="state">
        /// State of the entity to transfer authority of.
        /// </param>
        /// <param name="clientID">
        /// Client that should get authority over this entity
        /// Can be retrieved from <see cref="CoherenceBridge.ClientConnections"/>.
        /// </param>
        /// <param name="authorityTransferred">
        /// Type of authority transferred.
        /// </param>
        /// <returns>
        /// <see langowrd="true"/> if the transfer operation can be started.
        /// </returns>
        /// <see cref="NetworkEntityState.AuthorityType"/>
        /// <see cref="NetworkEntityState.HasStateAuthority"/>
        /// <see cref="NetworkEntityState.HasInputAuthority"/>
        public bool TransferAuthority(NetworkEntityState state, ClientID clientID, AuthorityType authorityTransferred = AuthorityType.Full)
        {
            if (!AssertNetworkEntityState(state))
            {
                return false;
            }

            if (state.Sync.AuthorityTransferTypeConfig == CoherenceSync.AuthorityTransferType.NotTransferable)
            {
                logger.Warning(Warning.ToolkitAuthorityManagerTransfer,
                    $"entityID: {state.EntityID} is NotTransferable, this authority transfer request will be cancelled.");
                return false;
            }

            if (state.IsMyClientConnection)
            {
                logger.Warning(Warning.ToolkitAuthorityManagerTransfer,
                    $"entityID: {state.EntityID} is the client connection, this authority transfer request will be cancelled.");
                return false;
            }

            if (state.IsOrphaned)
            {
                logger.Warning(Warning.ToolkitAuthorityManagerTransfer,
                    $"Cannot transfer {state.Sync.gameObject.name} because it is orphaned and you do not have authority over it.",
                    ("gameObject", state.Sync.gameObject));
                return false;
            }

            if (!state.AuthorityType.Value.CanTransfer(authorityTransferred) || authorityTransferred == AuthorityType.None)
            {
                logger.Debug("Authority transfer failed: insufficient authority",
                    ("localAuthority", state.AuthorityType.Value), ("transferredAuthority", authorityTransferred));
                return false;
            }

            if (client.ClientID == clientID && state.AuthorityType.Value.Contains(authorityTransferred))
            {
                logger.Warning(Warning.ToolkitAuthorityManagerTransfer,
                    "Attempting to transfer authority to self, this authority transfer request will be cancelled.",
                    ("localAuthority", state.AuthorityType.Value),
                    ("transferredAuthority", authorityTransferred));
                return false;
            }

            if (state.AuthorityType.Value.ControlsState())
            {
                // Send all the things.  Should probably make this a
                // Method in the Updater, really.
                state.Sync.SendConnectedEntity();
                state.Sync.ResetInterpolation();
                state.Sync.Updater.SendTag();
                // Sample bindings to send the latest values
                state.Sync.Updater.ManuallySendAllChanges(true);
            }

            return client.SendAuthorityTransfer(state.EntityID, clientID, true, authorityTransferred);
        }

        /// <summary>
        /// Transfers ownership of the entity to the replication server, making it an orphan.
        /// </summary>
        /// <remarks>
        /// The entity must be <see cref="CoherenceSync.LifetimeType.Persistent"/>, transferable (not <see cref="CoherenceSync.AuthorityTransferType.NotTransferable"/>), and the client must have state authority over it.
        /// The transfer fails if <see cref="NetworkEntityState.HasStateAuthority"/> is <see langword="false"/>, <see cref="CoherenceSync.lifetimeType"/> is not <see cref="CoherenceSync.LifetimeType.Persistent"/> or
        /// or <see cref="CoherenceSync.AuthorityTransfer"/> is <see cref="CoherenceSync.AuthorityTransferType.NotTransferable"/>.
        /// </remarks>
        /// <returns>
        /// <see langword="true"/> if the authority transfer was successful. <see langword="false"/> otherwise.
        /// </returns>
        public bool AbandonAuthority(NetworkEntityState state)
        {
            if (!AssertNetworkEntityState(state))
            {
                return false;
            }

            if (state.Sync.LifetimeTypeConfig != CoherenceSync.LifetimeType.Persistent)
            {
                logger.Warning(Warning.ToolkitAuthorityAbandon,
                    $"entityID: {state.EntityID} is not persistent, this abandon authority request will be cancelled.");
                return false;
            }

            if (state.AuthorityType.Value == AuthorityType.Input)
            {
                logger.Warning(Warning.ToolkitAuthorityAbandon,
                    $"entityID: {state.EntityID} cannot abandon just input authority.");
                return false;
            }

            if (!TransferAuthority(state, ClientID.Server, state.AuthorityType.Value))
            {
                return false;
            }

            state.IsOrphaned = true;

            // update the time so we don't immediately steal it again.
            state.LastTimeRequestedOrphanAdoption = Time.unscaledTime;

            return true;
        }

        /// <summary>
        /// Requests authority over an orphaned entity.
        /// </summary>
        /// <remarks>
        /// Adoption can only successfully start on orphaned entities.
        /// Even if the adoption can be started, the operation can be rejected.
        /// </remarks>
        /// <returns><see langword="true"/> if adoption can be started.</returns>
        /// <seealso cref="NetworkEntityState.IsOrphaned"/>
        public bool Adopt(NetworkEntityState state)
        {
            if (!AssertNetworkEntityState(state))
            {
                return false;
            }

            if (!state.IsOrphaned)
            {
                logger.Warning(Warning.ToolkitAuthorityAdopt,
                    $"entityID: {state.EntityID} cannot be adopted because it is not orphaned.");
                return false;
            }

            state.LastTimeRequestedOrphanAdoption = Time.unscaledTime;

            client.SendAdoptOrphanRequest(state.EntityID);
            return true;
        }

        private void HandleAuthorityRequest(Core.AuthorityRequest request)
        {
            logger.Debug($"Received authority request", ("entity", request.EntityID),
                ("requesterID", request.RequesterID), ("authorityType", request.AuthorityType));

            var state = bridge.GetNetworkEntityStateForEntity(request.EntityID);
            if (state == null)
            {
                return;
            }

            AuthorityRequestRespond respond = response =>
            {
                if (response.Accepted)
                {
                    state.Sync.Updater.ManuallySendAllChanges(true);
                }

                client.SendAuthorityTransfer(request.EntityID, request.RequesterID, response.Accepted, request.AuthorityType);
            };

            HandleAuthorityRequestInternal(state, new AuthorityRequest(request.RequesterID, request.AuthorityType, respond));
        }

        private void HandleAuthorityRequestRejected(AuthorityRequestRejection rejection)
        {
            (bridge.GetCoherenceSyncForEntity(rejection.EntityID) as CoherenceSync)?.RaiseOnAuthorityRequestRejected(rejection.AuthorityType);
        }

        private void HandleAuthorityChanged(AuthorityChange change)
        {
            var state = bridge.GetNetworkEntityStateForEntity(change.EntityID);
            if (state == null)
            {
                return;
            }

            state.AuthorityType.UpdateValue(change.NewAuthorityType);
        }

        private void HandleAuthorityTransferred(Entity entity)
        {
            (bridge.GetCoherenceSyncForEntity(entity) as CoherenceSync)?.RaiseOnAuthorityTranferred();
        }

        private void HandleAuthorityRequestInternal(NetworkEntityState state, AuthorityRequest request)
        {
            if (state.IsMyClientConnection)
            {
                request.Reject();

                return;
            }

            switch (state.Sync.AuthorityTransferTypeConfig)
            {
                case CoherenceSync.AuthorityTransferType.NotTransferable:
                    request.Reject();
                    return;

                case CoherenceSync.AuthorityTransferType.Request:
                    state.Sync.RaiseOnAuthorityRequested(request);
                    return;

                case CoherenceSync.AuthorityTransferType.Stealing:
                    request.Accept();
                    return;

                default:
                    throw new ArgumentException($"Unexpected AuthorityTransferType: {state.Sync.AuthorityTransferTypeConfig}");
            }
        }

        private bool AssertNetworkEntityState(NetworkEntityState state)
        {
            if (state == null)
            {
                logger.Error(Coherence.Log.Error.ToolkitAuthorityNullState);
                return false;
            }

            return true;
        }
    }
}

