// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Connection;
    using Core;
    using Entities;
    using ProtocolDef;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Logger = Log.Logger;

    public interface IEntitiesManager
    {
        IEnumerable<NetworkEntityState> NetworkEntities { get; }
    }

    /// <summary>
    /// Handles network entity state, and links network entities to Unity objects.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="CoherenceSync"/> and <see cref="CoherenceBridge"/>.
    /// </remarks>
    public class EntitiesManager : IEntitiesManager
    {
        /// <summary>
        /// Set of known entities.
        /// </summary>
        /// <seealso cref="EntityCount"/>
        public IEnumerable<NetworkEntityState> NetworkEntities => networkEntities.Values;

        /// <summary>
        /// Number of known entities.
        /// </summary>
        /// <seealso cref="NetworkEntities"/>
        public int EntityCount => networkEntities.Count;

        /// <summary>
        /// The entity that represents the connection.
        /// </summary>
        public Entity ConnectionEntityID => connectionEntityID;

        /// <summary> A set of known non-toolkit entities.</summary>
        private HashSet<Entity> internalEntities = new();

        private IClient client;
        private readonly ICoherenceBridge bridge;
        private readonly IClientConnectionManager clientConnectionsManager;
        private readonly CoherenceInputManager inputManager;
        private readonly UniquenessManager uniquenessManager;

        private readonly IDefinition definition;
        private readonly Logger logger;

        /// <summary>A dictionary of the existing network entities and their related runtime state, identified by their Entity.</summary>
        internal readonly Dictionary<Entity, NetworkEntityState> networkEntities = new();

        /// <summary>A dictionary of the existing network entities that we haven't started synchronizing yet.</summary>
        internal readonly Dictionary<string, Queue<UnsyncedNetworkEntity>> unsyncedNetworkEntities = new();

        /// <summary>A dictionary of the existing unique network entities that we haven't started synchronizing yet.</summary>
        internal readonly Dictionary<string, UnsyncedNetworkEntity> unsyncedNetworkEntitiesByUniqueId = new();

        /// <summary>
        /// The key is for what parent entity the update is depending on.
        /// The value is for all updates that depend on a given key entity.
        /// </summary>
        private readonly Dictionary<Entity, List<IncomingEntityUpdate>> delayedUpdatesDependingOnParents = new();

        /// <summary>
        /// When an entity is meant to be instantiated after being delayed it is added here to slowly
        /// process so that there isn't a large chain suddenly processed that causes stack issues.
        /// </summary>
        private readonly Stack<Entity> delayedEntitiesToInstantiate = new();

        /// <summary>
        /// Store Entity Updates that couldn't be applied because the target Entity has not been instantiated yet.
        /// </summary>
        private readonly Dictionary<Entity, List<IncomingEntityUpdate>> delayedEntityUpdates = new();

        /// <summary>
        /// Store Entity commands that couldn't be applied because the target Entity has not been instantiated yet.
        /// </summary>
        private readonly Dictionary<Entity, List<(IEntityCommand command, MessageTarget target)>> delayedEntityCommands = new();

        /// <summary>A dictionary that keeps track of which point in the update loop an entity should process interpolation and callbacks.</summary>
        private readonly CoherenceLoopLookup coherenceLoopLookup = new CoherenceLoopLookup();

        private Entity connectionEntityID;

        internal EntitiesManager()
        {
        }

        internal EntitiesManager(ICoherenceBridge bridge,
            IClientConnectionManager clientConnectionsManager,
            CoherenceInputManager inputManager,
            UniquenessManager uniquenessManager,
            IDefinition definition,
            Logger logger)
        {
            this.definition = definition;
            this.logger = logger.With<EntitiesManager>();

            this.bridge = bridge;
            this.clientConnectionsManager = clientConnectionsManager;
            this.inputManager = inputManager;
            this.uniquenessManager = uniquenessManager;

            SetClient(bridge.Client);
        }

        /// <summary>
        /// Gets a CoherenceSync for a given entity, from the known set of entities.
        /// </summary>
        /// <param name="id">A known existing entity.</param>
        /// <returns>The CoherenceSync associated with that entity. <see langword="null"/> if the entity is not found.</returns>
        /// <seealso cref="GetNetworkEntityStateForEntity"/>
        public ICoherenceSync GetCoherenceSyncForEntity(Entity id)
        {
            if (!TryGetNetworkEntityState(id, out var state))
            {
                logger.Trace($"We can't find a CoherenceSync for EntityID {id} in the mapper (we will try to set up any references to that entity later, if it is found.)");

                return null;
            }

            return state.Sync;
        }

        /// <summary>
        /// Gets the entity state for a given entity, from the known set of entities.
        /// </summary>
        /// <param name="id">A known existing entity.</param>
        /// <returns>The state associated with that entity. <see langword="null"/> if the entity is not found.</returns>
        /// <seealso cref="GetNetworkEntityStateForEntity"/>
        public NetworkEntityState GetNetworkEntityStateForEntity(Entity id)
        {
            if (!TryGetNetworkEntityState(id, out var state))
            {
                logger.Trace($"We can't find a CoherenceSync for EntityID {id} in the mapper (we will try to set up any references to that entity later, if it is found.)");

                return null;
            }

            return state;
        }

        internal void InterpolateBindings(CoherenceSync.InterpolationLoop interpolationLoop)
        {
            foreach (var updater in coherenceLoopLookup.Get(interpolationLoop).Values)
            {
                updater.InterpolateBindings();
            }
        }

        internal void InvokeCallbacks(CoherenceSync.InterpolationLoop interpolationLoop)
        {
            foreach (var updater in coherenceLoopLookup.Get(interpolationLoop).Values)
            {
                updater.InvokeCallbacks();
            }
        }

        internal void SampleBindings(CoherenceSync.InterpolationLoop interpolationLoop)
        {
            foreach (var updater in coherenceLoopLookup.Get(interpolationLoop).Values)
            {
                updater.SampleBindings();
            }
        }

        internal void SyncAndSend()
        {
            foreach (var entity in networkEntities.Values)
            {
                entity.Sync?.Updater?.SyncAndSend();
            }
        }

        /// <inheritdoc cref="UnityObjectToEntityId(CoherenceSync)"/>
        public static Entity UnityObjectToEntityId(GameObject from)
        {
            if (from == null)
            {
                return default;
            }

            var fromSync = from.GetComponent<ICoherenceSync>();
            return UnityObjectToEntityId(fromSync);
        }

        /// <inheritdoc cref="UnityObjectToEntityId(CoherenceSync)"/>
        public static Entity UnityObjectToEntityId(Component from)
        {
            if (!from)
            {
                return default;
            }

            var fromSync = from.GetComponent<ICoherenceSync>();
            return UnityObjectToEntityId(fromSync);
        }

        /// <summary>
        /// Gets the entity associated with a <see cref="CoherenceSync"/>.
        /// </summary>
        public Entity UnityObjectToEntityId(Transform from)
        {
            if (!from)
            {
                return default;
            }

            var fromSync = from.GetComponent<ICoherenceSync>();
            return UnityObjectToEntityId(fromSync);
        }

        /// <summary>
        /// Gets the entity associated with a <see cref="CoherenceSync"/>.
        /// </summary>
        public static Entity UnityObjectToEntityId(ICoherenceSync from) => from?.EntityState?.EntityID ?? Entity.InvalidRelative;

        /// <summary>
        /// Get the <see cref="GameObject"/> associated with an <see cref="Entity"/>.
        /// </summary>
        public GameObject EntityIdToGameObject(Entity from)
        {
            if (from == Entity.InvalidRelative)
            {
                return null;
            }

            var toSync = GetCoherenceSyncForEntity(from);

            return toSync?.gameObject;
        }

        /// <summary>
        /// Get the <see cref="Transform"/> associated with an <see cref="Entity"/>.
        /// </summary>
        public Transform EntityIdToTransform(Entity from)
        {
            if (from == Entity.InvalidRelative)
            {
                return null;
            }

            var toSync = GetCoherenceSyncForEntity(from);

            return toSync?.transform;
        }

        /// <summary>
        /// Get the <see cref="RectTransform"/> associated with an <see cref="Entity"/>.
        /// </summary>
        public RectTransform EntityIdToRectTransform(Entity from)
        {
            if (from == Entity.InvalidRelative)
            {
                return null;
            }

            var toSync = GetCoherenceSyncForEntity(from);

            return toSync?.transform as RectTransform;
        }

        /// <summary>
        /// Get the <see cref="ICoherenceSync"/> associated with an <see cref="Entity"/>.
        /// </summary>
        public ICoherenceSync EntityIdToCoherenceSync(Entity from)
        {
            if (from == Entity.InvalidRelative)
            {
                return null;
            }

            return GetCoherenceSyncForEntity(from);
        }

        /// <summary>
        /// Set the active Unity scene based on the associated <see cref="CoherenceBridge"/>.
        /// </summary>
        /// <remarks>
        /// Scene is set via <see cref="SceneManager.SetActiveScene"/>.
        /// If <see cref="CoherenceBridge.InstantiationScene"/> is set and is loaded, that scene will be set as active.
        /// Otherwise, the active scene won't be changed.
        /// </remarks>
        public Scene? SetActiveScene()
        {
            Scene? lastActiveScene = null;

            var targetScene = bridge.InstantiationScene;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene != targetScene && targetScene is { isLoaded: true })
            {
                lastActiveScene = activeScene;
                SceneManager.SetActiveScene(targetScene.Value);
            }

            return lastActiveScene;
        }

        internal bool TryGetNetworkEntityState(Entity id, out NetworkEntityState networkEntityState)
        {
            return networkEntities.TryGetValue(id, out networkEntityState);
        }

        /// <summary>
        /// Enumerates the known network entities.
        /// </summary>
        /// <seealso cref="networkEntities"/>
        public Dictionary<Entity, NetworkEntityState>.Enumerator GetEnumerator()
        {
            return networkEntities.GetEnumerator();
        }

        internal virtual (NetworkEntityState, ComponentUpdates?, uint?, bool) SyncNetworkEntityState(ICoherenceSync sync)
        {
            if (!client.IsConnected())
            {
                return (null, null, null, false);
            }

            var assetId = sync.CoherenceSyncConfig?.ID ?? string.Empty;
            var uniqueId = sync.ManualUniqueId;

            var result = ClaimUnsyncedNetworkEntity(assetId, uniqueId);

            if (FoundUnsyncedNetworkEntity(result))
            {
                if (uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(sync, result.Item2.EntityState.CoherenceUUID))
                {
                    return (null, null, null, false);
                }

                // This happens when a unique local sync is resolved to
                // actually be remote. The sync now needs to switch state from locally created
                // to remotely created. And the old state needs to be removed from the mapper,
                // even though the duplicate destroy will do this as well, but if we doin't
                // do it now, the mapper will be in an inconsistent state for a moment.
                if (sync.EntityState != null)
                {
                    sync.EntityState.Sync = null;
                    RemoveEntityFromMapper(sync.EntityState);
                }

                result.Item2.EntityState.Sync = sync;
                AddNetworkEntityStateToMapper(result.Item2.EntityState, sync);

                InstantiateDelayedEntitiesDependingOnThisParent(result.Item2.EntityState.EntityID);

                return (result.Item2.EntityState, result.Item2.Updates, result.Item2.LOD, false);
            }

            if (DisableServerObjectIfClient(sync))
            {
                return (null, null, null, true);
            }

            var uuid = string.Empty;

            if (sync.IsUnique)
            {
                uuid = sync.ManualUniqueId;

                if (uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(sync, uuid))
                {
                    return (null, null, null, false);
                }
            }

            var entityState = CreateEntity(sync, uuid);

            AddNetworkEntityStateToMapper(entityState, sync);

            return (entityState, null, null, false);
        }

        internal void ApplyDelayedUpdates(Entity id)
        {
            if (delayedEntityUpdates.TryGetValue(id, out var updates))
            {
                foreach (var update in updates)
                {
                    UpdateNetworkedEntity(id, update);
                }
            }

            delayedEntityUpdates.Remove(id);
        }

        internal bool AddDelayedCommand(IEntityCommand command, MessageTarget target, Entity id)
        {
            if (networkEntities.ContainsKey(id) || internalEntities.Contains(id))
            {
                return false;
            }

            logger.Debug("Received command for an unknown Toolkit entity.\n" +
                "Storing the Update to be applied when the Entity is instantiated.",
                ("EntityID", id), ("Type", command.GetComponentType()));

            if (!delayedEntityCommands.TryGetValue(id, out var commands))
            {
                commands = new List<(IEntityCommand command, MessageTarget target)>();
                delayedEntityCommands.Add(id, commands);
            }

            commands.Add((command, target));

            return true;
        }

        internal void ApplyDelayedCommands(Entity id)
        {
            if (!TryGetNetworkEntityState(id, out var state))
            {
                logger.Error(Log.Error.ToolkitEntitiesManagerDelayedCommands,
                    ("id", id));

                return;
            }

            if (delayedEntityCommands.TryGetValue(id, out var commands))
            {
                var sync = state.Sync;

                foreach (var commandInfo in commands)
                {
                    sync.ReceiveCommand(commandInfo.command, commandInfo.target);
                }
            }

            delayedEntityCommands.Remove(id);
        }

        private bool DisableServerObjectIfClient(ICoherenceSync sync)
        {
            if (sync.SimulationTypeConfig != CoherenceSync.SimulationType.ServerSide)
            {
                return false;
            }

            return !SimulatorUtility.IsSimulator && !bridge.IsSimulatorOrHost;
        }

        internal void DestroyAuthorityNetworkEntityState(NetworkEntityState state)
        {
            if (state == null)
            {
                return;
            }

            RemoveEntityFromMapper(state);

            if (state.HasStateAuthority)
            {
                client.DestroyEntity(state.EntityID);
            }
        }

        internal void UpdateInterpolationLoopConfig(ICoherenceSync sync, CoherenceSync.InterpolationLoop newLocation)
        {
            coherenceLoopLookup.Remove(sync.EntityState.EntityID, sync.InterpolationLocationConfig);
            coherenceLoopLookup.Add(sync.EntityState.EntityID, sync.Updater, newLocation);
        }

        internal bool ContainsEntity(Entity id)
        {
            return networkEntities.ContainsKey(id) || internalEntities.Contains(id);
        }

        internal void Update()
        {
            if (delayedEntitiesToInstantiate.TryPop(out var parentEntity))
            {
                var updatesDependingOnThisEntity = delayedUpdatesDependingOnParents[parentEntity];

                _ = delayedUpdatesDependingOnParents.Remove(parentEntity);

                foreach (var delayedUpdate in updatesDependingOnThisEntity)
                {
                    CreateNetworkedEntity(delayedUpdate.Meta.EntityId, delayedUpdate, out _);
                }
            }
        }

        private void SetClient(IClient client)
        {
            this.client = client;
            client.OnEntityCreated += (id, update) => CreateNetworkedEntity(id, update, out _);
            client.OnEntityUpdated += UpdateNetworkedEntity;
            client.OnEntityDestroyed += DestroyNetworkedEntity;
            client.OnDisconnected += HandleDisconnected;
        }

        private (bool, UnsyncedNetworkEntity) ClaimUnsyncedNetworkEntity(string assetId, string uniqueId)
        {
            if (!string.IsNullOrEmpty(uniqueId) && unsyncedNetworkEntitiesByUniqueId.TryGetValue(uniqueId, out var unsyncedNetworkEntity))
            {
                unsyncedNetworkEntitiesByUniqueId.Remove(uniqueId);
                return (true, unsyncedNetworkEntity);
            }

            if (unsyncedNetworkEntities.TryGetValue(assetId, out var queue) && queue.Count > 0)
            {
                return (true, queue.Dequeue());
            }

            return (false, default);
        }

        private static bool FoundUnsyncedNetworkEntity((bool, UnsyncedNetworkEntity) result)
        {
            return result.Item1;
        }

        private NetworkEntityState CreateEntity(ICoherenceSync sync, string uuid)
        {
            var components = new List<ICoherenceComponentData>();

            var isNestedPrefab = sync.IsChildFromSyncGroup() && !string.IsNullOrEmpty(uuid);
            var initialComps = Impl.CreateInitialComponents(sync, uuid, isNestedPrefab, bridge.NetworkTime.ClientSimulationFrame);

            components.AddRange(initialComps);

            if (sync.BakedScript != null)
            {
                sync.BakedScript.CreateEntity(sync.UsesLODsAtRuntime, sync.ArchetypeName, bridge.NetworkTime.ClientSimulationFrame, components);
            }

            // Not sampling here because this causes multiple samples in the buffer
            // and the replicated clients could then use an interpolated value
            // for the initial value.  Leaving this comment here since the effects of
            // this interpolation are hard to detect and make the test
            // Sync_InitialStateWhenSpawnedViaCommand flaky.
            // This change presumes the initial sample sent with the create is handled
            // in the LateUpdate sample which should always happen for new entities.

            var serializeEntity = client.CreateEntity(components.ToArray(), false);

            logger.Trace($"Created new entity locally for asset {sync.CoherenceSyncConfig.ID} and uuid {uuid}");

            return new NetworkEntityState(serializeEntity, AuthorityType.Full, false, false, sync, uuid);
        }

        private void HandleDisconnected(ConnectionCloseReason obj)
        {
            var entitiesToBeDestroyed = new List<NetworkEntityState>(networkEntities.Values);

            foreach (var entity in entitiesToBeDestroyed)
            {
                try
                {
                    var sync = entity.Sync;
                    if (entity.AuthorityType.Value.ControlsState())
                    {
                        sync?.HandleDisconnected();
                    }
                    else
                    {
                        sync?.CoherenceSyncConfig.Instantiator.Destroy(sync);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(Log.Error.ToolkitEntitiesManagerHandleDisconnected,
                        $"An exception occured in {nameof(HandleDisconnected)} " +
                        $"with entity {entity.Sync} ({entity.CoherenceUUID})" +
                        $"with {nameof(AuthorityType)} {entity.AuthorityType.Value}.\n{e}");
                }
            }

            networkEntities.Clear();
            delayedEntityUpdates.Clear();
            delayedEntityCommands.Clear();
            coherenceLoopLookup.Clear();
            internalEntities.Clear();
            unsyncedNetworkEntities.Clear();
            unsyncedNetworkEntitiesByUniqueId.Clear();
            connectionEntityID = Entity.InvalidRelative;
        }

        private void CreateNetworkedEntity(Entity entityID, IncomingEntityUpdate entityUpdate, out bool shouldSpawn)
        {
            var (spawn, info) = Impl.GetSpawnInfo(client, entityUpdate, logger);
            shouldSpawn = spawn;

            if (!shouldSpawn)
            {
                internalEntities.Add(entityID);
                logger.Debug("Create for non-toolkit entity",
                    ("EntityID", entityID),
                    ("Components", entityUpdate.Components));
                return;
            }

            bool isConnectionEntity = info.clientId.HasValue && info.connectionType.HasValue;
            if (isConnectionEntity)
            {
                internalEntities.Add(entityID);

                var isLocalClient = entityUpdate.Meta.HasStateAuthority;
                if (isLocalClient)
                {
                    connectionEntityID = entityID;
                }

                if (!bridge.EnableClientConnections)
                {
                    return;
                }

                clientConnectionsManager.GetPrefab(bridge.ClientID, info.connectionType.Value, (sync) =>
                {
                    if (sync != null)
                    {
                        info.assetId = sync.CoherenceSyncConfig.ID;
                        InstantiateNetworkedEntity(sync, info, entityID, entityUpdate);
                    }
                    else
                    {
                        var connection = new CoherenceClientConnection(
                            bridge,
                            entityID,
                            info.clientId.Value,
                            info.connectionType.Value,
                            entityUpdate.Meta.HasStateAuthority);

                        clientConnectionsManager.Add(connection);
                    }
                });
            }
            else
            {
                var registry = CoherenceSyncConfigRegistry.Instance;
                if (registry.TryGetFromAssetId(info.assetId, out var config))
                {
                    config.Provider.LoadAsset(info.assetId,
                        sync => InstantiateNetworkedEntity(sync, info, entityID, entityUpdate));
                }
            }
        }

        private void UpdateNetworkedEntity(Entity id, IncomingEntityUpdate entityUpdate)
        {
            if (TryGetNetworkEntityState(id, out var state))
            {
                var sync = state.Sync;
                state.IsOrphaned = entityUpdate.Meta.IsOrphan;
                sync.Updater.ApplyComponentDestroys(entityUpdate.Components.Destroys);

                var updates = entityUpdate.Components.Updates;
                RemoveUnhandledComponents(updates);
                entityUpdate.Components.Updates = updates;  //replace with updated updates.
                sync.Updater.ApplyComponentUpdates(updates);

                if (sync.UsesLODsAtRuntime)
                {
                    sync.SetObservedLodLevel((int)entityUpdate.Meta.LOD);
                }
            }
            else if (!internalEntities.Contains(id))
            {
                logger.Debug("Received update for an unknown Toolkit entity. Storing the Update to be applied when the Entity is instantiated.", ("EntityID", id), ("Comps", entityUpdate));

                if (!delayedEntityUpdates.TryGetValue(id, out var updates))
                {
                    updates = new List<IncomingEntityUpdate>(1);
                    delayedEntityUpdates.Add(id, updates);
                }

                updates.Add(entityUpdate);
            }
        }

        private void DestroyNetworkedEntity(Entity entityID, DestroyReason destroyReason)
        {
            if (entityID == Entity.InvalidRelative)
            {
                logger.Warning(Log.Warning.ToolkitEntitiesManagerRemoteEntityInvalid,
                    $"We can't delete an entity with ID 'default', this call to '{nameof(DestroyNetworkedEntity)}' will be ignored.",
                    ("context", this));
                return;
            }

            clientConnectionsManager.Remove(entityID);

            if (internalEntities.Remove(entityID))
            {
                logger.Debug("Destroy for non-toolkit entity", ("entity", entityID), ("reason", destroyReason));
            }

            if (!TryGetNetworkEntityState(entityID, out var state))
            {
                logger.Trace($"Unable to delete entity [{entityID}] with reason: {destroyReason} because it is not mapped.", ("context", this));

                return;
            }

            var sync = state.Sync;

            if (destroyReason == DestroyReason.MaxEntitiesReached)
            {
                logger.Warning(Log.Warning.ToolkitEntitiesManagerMaxEntities,
                    $"Max entity count exceeded. Entity '{sync.gameObject.name}'[{entityID}] will be destroyed.",
                    ("context", this));
            }
            else if (destroyReason == DestroyReason.MaxQueriesReached)
            {
                logger.Warning(Log.Warning.ToolkitEntitiesManagerMaxQueries,
                    $"Max query count exceeded. Entity `{sync.gameObject.name}`[{entityID}] with the query will be destroyed. " +
                    $"See: {DocumentationLinks.GetDocsUrl(DocumentationKeys.MaxQueryCount)}",
                    ("context", this));
            }
            else if (destroyReason == DestroyReason.UnauthorizedCreate)
            {
                logger.Warning(Log.Warning.ToolkitEntitiesManagerUnauthorizedCreate,
                    $"This client is unauthorized to create entities because HostAuthority.CreateEntities feature is enabled. " +
                    $"Entity '{sync.gameObject.name}' [{entityID}] will be destroyed",
                    ("context", this));
            }
            else if (destroyReason == DestroyReason.DuplicateDestroy)
            {
                // Note: sync can be null if the entity was already replaced by the duplicate create
                // before the destroy arrived.
                if (sync == null)
                {
                    RemoveEntityFromMapper(state);

                    return;
                }

                // If the duplicate is a child of a sync group then don't disable or delete
                // since that is handled by the parent, just do the replacement.
                if (!sync.IsChildFromSyncGroup())
                {
                    if (sync.ReplacementStrategy == CoherenceSync.UniqueObjectReplacementStrategy.Destroy)
                    {
                        DestroyLocalObject(state, destroyReason);
                    }
                    else
                    {
                        // if we don't destroy, we have to disable until the create for the
                        // official version arrives.  Leaving it active can just lead to
                        // an unsynced instance disconnected from the network.
                        sync.gameObject.SetActive(false);
                    }
                }

                if (uniquenessManager.ReplaceRemoteDuplicatedEntity(sync, state))
                {
                    RemoveEntityFromMapper(state);
                }

                return;
            }

            DestroyLocalObject(state, destroyReason);
            RemoveEntityFromMapper(state);
        }

        private void DestroyLocalObject(NetworkEntityState state, DestroyReason destroyReason)
        {
            if (state.Sync.IsChildFromSyncGroup())
            {
                // If its part of a group it will be destroyed by the parent
                return;
            }

            bridge.OnNetworkEntityDestroyedInvoke(state, destroyReason);

            state.Sync.HandleNetworkedDestruction(false);
        }

        private void AddNetworkEntityStateToMapper(NetworkEntityState state, ICoherenceSync sync)
        {
            var id = state.EntityID;

            if (id == Entity.InvalidRelative)
            {
                logger.Warning(Log.Warning.ToolkitEntitiesManagerMapperInvalid,
                    $"We can't add the CoherenceSync '{state.Sync}' to the mapper, because it has the EntityID {Entity.InvalidRelative}.",
                    ("context", state.Sync));
                return;
            }

            if (networkEntities.ContainsKey(id))
            {
                logger.Warning(Log.Warning.ToolkitEntitiesManagerMapperDuplicate,
                    $"We can't add CoherenceSync '{state.Sync}' to the mapper, because the EntityID {id} already exists.",
                    ("context", state.Sync));
                return;
            }

            networkEntities[id] = state;

            coherenceLoopLookup.Add(state.EntityID, state.Sync.Updater, state.Sync.InterpolationLocationConfig);

            if (state.Sync.HasInput)
            {
                inputManager.AddInput(state.Sync.Input);
            }
        }

        private void RemoveEntityFromMapper(NetworkEntityState state)
        {
            if (state == null)
            {
                return;
            }

            networkEntities.Remove(state.EntityID);
            delayedEntityUpdates.Remove(state.EntityID);
            delayedEntityCommands.Remove(state.EntityID);

            coherenceLoopLookup.Remove(state.EntityID, (CoherenceSync.InterpolationLoop)0xff);

            if (state.Sync?.HasInput ?? false)
            {
                inputManager.RemoveInput(state.Sync.Input);
            }
        }

        private void InstantiateNetworkedEntity(ICoherenceSync syncPrefab, SpawnInfo info, Entity entityID, IncomingEntityUpdate entityUpdate)
        {
            Scene? lastActiveScene = null;
            try
            {
                lastActiveScene = SetActiveScene();
                NetworkEntityState entityState = null;

                if (info.isFromGroup)
                {
                    entityState = CreateUnsynchronizedNetworkEntity(info, entityID, entityUpdate, syncPrefab.UnsyncedEntityPriority);
                }

                if (DelayInstantiationDependingOnParent(info.connectedEntity, entityUpdate))
                {
                    return;
                }

                if (!info.isFromGroup)
                {
                    entityState = CreateUnsynchronizedNetworkEntity(info, entityID, entityUpdate, syncPrefab.UnsyncedEntityPriority);
                }

                if (uniquenessManager.FindUniqueObjectForNewRemoteNetworkEntity(info, () =>
                    {
                        if (TryGetNetworkEntityState(entityID, out var state))
                        {
                            RemoveEntityFromMapper(state);
                        }
                    }))
                {
                    PostNetworkEntityCreationActions(entityState);
                    return;
                }

                info.ComponentUpdates = entityUpdate.Components.Updates;
                InstantiateCoherenceSync(syncPrefab, info, entityState);
            }
            catch (Exception e)
            {
                logger.Error(Log.Error.ToolkitEntitiesManagerInstantiationException,
                    $"The instantiation of the prefab '{syncPrefab}' failed: {e}",
                    ("context", syncPrefab));
            }
            finally
            {
                if (lastActiveScene.HasValue)
                {
                    SceneManager.SetActiveScene(lastActiveScene.Value);
                }
            }
        }

        private bool DelayInstantiationDependingOnParent(Entity parentEntity, IncomingEntityUpdate entityUpdate)
        {
            if (!parentEntity.IsValid || networkEntities.ContainsKey(parentEntity))
            {
                return false;
            }

            if (!delayedUpdatesDependingOnParents.ContainsKey(parentEntity))
            {
                delayedUpdatesDependingOnParents[parentEntity] = new List<IncomingEntityUpdate> { entityUpdate };
            }
            else
            {
                delayedUpdatesDependingOnParents[parentEntity].Add(entityUpdate);
            }

            // Will instantiate this entity later, after the parent has been instantiated.
            return true;

        }

        private void InstantiateCoherenceSync(ICoherenceSync syncPrefab, SpawnInfo info, NetworkEntityState state)
        {
            CoherenceBridgeStore.instantiatingBridge = bridge;
            try
            {
                if (!info.isFromGroup)
                {
                    info.bridge = bridge;
                    info.prefab = syncPrefab;
                    info.rotation ??= (syncPrefab as MonoBehaviour)?.transform.rotation ?? Quaternion.identity;

                    var config = syncPrefab.CoherenceSyncConfig;
                    config.Instantiator.Instantiate(info);
                }

                PostNetworkEntityCreationActions(state);
            }
            finally
            {
                CoherenceBridgeStore.instantiatingBridge = null;
            }
        }

        private NetworkEntityState CreateUnsynchronizedNetworkEntity(SpawnInfo info, Entity entityID,
            IncomingEntityUpdate entityUpdate, CoherenceSync.UnsyncedNetworkEntityPriority unsyncedNetworkEntityPriority)
        {
            var networkEntity =
                new NetworkEntityState(entityID, entityUpdate.Meta.Authority(), entityUpdate.Meta.IsOrphan, true, null, info.uniqueId);

            var isConnectionEntity = info.clientId.HasValue && info.connectionType.HasValue;
            var connection = isConnectionEntity ? new CoherenceClientConnection(bridge, networkEntity, entityID,
                info.clientId.Value, info.connectionType.Value) : null;
            networkEntity.ClientConnection = connection;

            var components = entityUpdate.Components.Updates;
            RemoveUnhandledComponents(components);

            var unsyncedNetworkEntity = new UnsyncedNetworkEntity()
            {
                EntityState = networkEntity,
                Updates = entityUpdate.Components.Updates,
                LOD = entityUpdate.Meta.LOD,
                UniqueUUID = info.uniqueId
            };

            if ((unsyncedNetworkEntityPriority == CoherenceSync.UnsyncedNetworkEntityPriority.AssetId || string.IsNullOrEmpty(info.uniqueId))
                && !info.isFromGroup)
            {
                if (!unsyncedNetworkEntities.TryGetValue(info.assetId, out Queue<UnsyncedNetworkEntity> queue))
                {
                    queue = new Queue<UnsyncedNetworkEntity>();
                    unsyncedNetworkEntities.Add(info.assetId, queue);
                }

                queue.Enqueue(unsyncedNetworkEntity);
            }
            else
            {
                unsyncedNetworkEntitiesByUniqueId[info.uniqueId] = unsyncedNetworkEntity;
            }

            return networkEntity;
        }

        private void PostNetworkEntityCreationActions(NetworkEntityState state)
        {
            try
            {
                if (state.ClientConnection != null)
                {
                    CoroutineRunner.StartCoroutine(AddClientConnection(state.ClientConnection));
                }

                bridge.OnNetworkEntityCreatedInvoke(state);
            }
            catch (Exception handlerException)
            {
                logger.Error(Log.Error.ToolkitEntitiesManagerPostNetworkCreateException,
                    $"{nameof(bridge.OnNetworkEntityCreatedInvoke)} exception in handler: {handlerException}");
            }
        }

        private void InstantiateDelayedEntitiesDependingOnThisParent(Entity parentEntity)
        {
            if (!delayedUpdatesDependingOnParents.ContainsKey(parentEntity))
            {
                return;
            }

            delayedEntitiesToInstantiate.Push(parentEntity);
        }

        private IEnumerator AddClientConnection(CoherenceClientConnection clientConnection)
        {
            // We delay registering client connection by 1 frame, to make sure the related prefab instance is fully initialized (Awake/OnEnable/Start)
            yield return null;

            clientConnectionsManager.Add(clientConnection);
        }

        private static void RemoveUnhandledComponents(ComponentUpdates componentUpdates)
        {
            // The GenericPrefabReference/Id aren't bindings
            componentUpdates.Remove(Impl.AssetId());
        }
    }
}
