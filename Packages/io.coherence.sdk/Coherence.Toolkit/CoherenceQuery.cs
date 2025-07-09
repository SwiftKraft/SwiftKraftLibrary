// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Coherence;
    using Connection;
    using Entities;
    using Log;
    using Logger = Log.Logger;

    [NonBindable]
    public abstract class CoherenceQuery : CoherenceBehaviour
    {
        /// <inheritdoc cref="CoherenceBridgeResolver{T}"/>
        public event CoherenceBridgeResolver<CoherenceQuery> BridgeResolve;

        public Entity EntityID { get; set; }

        protected CoherenceBridge bridge;
        protected IClient Client => bridge.Client;
        protected Logger Logger { get; private set; }

        protected bool IsConnected => bridge != null && bridge.IsConnected;
        protected bool directlyCreatedEntity;

        private CoherenceSync sync;

        protected virtual void Awake()
        {
            sync = GetComponentInParent<CoherenceSync>();

            // Try to attach the component as early as possible so
            // it can be sent with the create.
            if (sync?.CoherenceBridge?.IsConnected ?? false)
            {
                bridge = sync.CoherenceBridge;
                OnConnected(sync.CoherenceBridge);
            }
        }

        private void Start()
        {
            Logger = Log.GetLogger<CoherenceQuery>(gameObject.scene);

            if (bridge == null && !CoherenceBridgeStore.TryGetBridge(gameObject.scene, BridgeResolve, this, out bridge))
            {
                enabled = false;
                return;
            }

            bridge.OnAfterFloatingOriginShifted += OnFloatingOriginShiftedInternal;
            bridge.onConnected.AddListener(OnConnected);
            bridge.onDisconnected.AddListener(OnDisconnected);

            Client.OnEntityDestroyed += OnEntityDestroyed;

            if (IsConnected)
            {
                OnConnected(bridge);
            }
        }

        private void OnConnected(CoherenceBridge _)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            CreateQuery();
            UpdateQuery();
        }

        private void OnEntityDestroyed(Entity entity, DestroyReason destroyReason)
        {
            if (entity != EntityID)
            {
                return;
            }

            if (sync != null)
            {
                // The entity is destroyed by the CoherenceSync so we
                // don't have to handle checking for the reason.
                return;
            }

            if (destroyReason == DestroyReason.MaxEntitiesReached)
            {
                Logger.Warning(Warning.ToolkitEntitiesManagerMaxEntities,
                    $"Max entity count exceeded. Query on '{gameObject.name}' will be disabled.",
                    ("context", this));
            }
            else if (destroyReason == DestroyReason.MaxQueriesReached)
            {
                Logger.Warning(Warning.ToolkitEntitiesManagerMaxQueries,
                    $"Max query count exceeded. Query on  `{gameObject.name}` will be disabled. " +
                    $"See: {DocumentationLinks.GetDocsUrl(DocumentationKeys.MaxQueryCount)}",
                    ("context", this));
            }
            else if (destroyReason == DestroyReason.UnauthorizedCreate)
            {
                Logger.Warning(Warning.ToolkitEntitiesManagerUnauthorizedCreate,
                    $"This client is unauthorized to create entities because HostAuthority.CreateEntities feature is enabled. " +
                    $"Query on '{gameObject.name}' will be disabled",
                    ("context", this));
            }

            enabled = false;
        }

        private void OnDisconnected(CoherenceBridge _, ConnectionCloseReason __)
        {
            // on disconnect, the client will have deleted all the entities internally
            // so we can clear the entity ID and the fact that we even directly
            // created one so that the next connect is handled properly.
            EntityID = Entity.InvalidRelative;
            directlyCreatedEntity = false;
        }

        private void Update()
        {
            if (!IsConnected)
            {
                return;
            }

            if (NeedsUpdate)
            {
                CreateQuery();
                UpdateQuery();
            }
        }

        private void OnEnable()
        {
            if (!IsConnected)
            {
                return;
            }

            CreateQuery();
            UpdateQuery();
        }

        private void OnDisable()
        {
            if (!IsConnected)
            {
                return;
            }

            UpdateQuery(false);
        }

        private void OnDestroy()
        {
            if (bridge == null)
            {
                return;
            }

            bridge.OnAfterFloatingOriginShifted -= OnFloatingOriginShiftedInternal;
            bridge.onConnected.RemoveListener(OnConnected);
            bridge.onDisconnected.RemoveListener(OnDisconnected);

            Client.OnEntityDestroyed -= OnEntityDestroyed;

            // If the entity was directly created, we inform the client to destroy
            // it, otherwise this is part of a CoherenceSync prefab, and the CS will
            // handle destruction.
            if (directlyCreatedEntity && EntityID != Entity.InvalidRelative)
            {
                Client.DestroyEntity(EntityID);
            }
        }

        private void OnFloatingOriginShiftedInternal(FloatingOriginShiftArgs args)
        {
            if (!IsConnected)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                CreateQuery();
                UpdateQuery();
            }
        }

        private void CreateQuery()
        {
            if (EntityID != Entity.InvalidRelative)
            {
                return;
            }

            if (sync?.EntityState != null)
            {
                EntityID = sync.EntityState.EntityID;
            }
            else
            {
                EntityID = Client.CreateEntity(new ICoherenceComponentData[] { }, false);
                directlyCreatedEntity = true;
            }
        }

        protected abstract bool NeedsUpdate { get; }
        protected abstract void UpdateQuery(bool queryActive = true);
    }
}
