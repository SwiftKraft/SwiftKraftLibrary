// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Connection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Archetypes;
    using UnityEngine;
    using UnityEngine.Events;
    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;
    using Entities;
    using Bindings;
    using Bindings.TransformBindings;
    using ProtocolDef;
    using Log;
    using System.Collections;
    using System.ComponentModel;
    using UnityEngine.Serialization;
    using Logger = Log.Logger;
    using Common;
    using UnityEngine.PlayerLoop;
    using Component = UnityEngine.Component;

    /// <summary>
    /// Represents an entity on the network.
    /// </summary>
    /// <remarks>
    /// Add this component to a <see cref="GameObject"/> and start networking by adding <see cref="Bindings"/>.
    ///
    /// Check the documentation for tutorials, guides, and more information.
    /// </remarks>
    [AddComponentMenu("coherence/Coherence Sync")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceSync)]
    [DisallowMultipleComponent]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.7/manual/components/coherence-sync")]
    public sealed partial class CoherenceSync : CoherenceBehaviour, ICoherenceSync, IConnectedEntityDriver
    {
        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceSync()
        {
        }

        /// <summary>
        /// How this entity should respond to requests for authority (via <see cref="RequestAuthority(AuthorityType)"/>).
        /// </summary>
        public enum AuthorityTransferType
        {
            /// <summary>
            /// This entity can't be transferred to other clients.
            /// </summary>
            /// <remarks>
            /// This setting is not available in some
            /// circumstances since that would lead to orphaned entities
            /// that cannot ever be adopted again.
            /// </remarks>
            [InspectorName("Not transferable")] NotTransferable = 0,

            /// <summary>
            /// Authority transfer can be requested by other clients.
            /// </summary>
            /// <remarks>
            /// Approval is resolved in the following order: <see cref="CoherenceSync.OnAuthorityRequest"/>, otherwise <see cref="CoherenceSync.OnAuthorityRequested"/>, otherwise <see cref="CoherenceSync.approveAuthorityTransferRequests"/>.
            /// </remarks>
            Request = 1,

            /// <summary>
            /// Always accept requests for authority.
            /// </summary>
            [InspectorName("Steal")] Stealing = 2,
        }

        /// <summary>
        /// The AuthorityTransfer configuration which is mapped into <see cref="AuthorityTransferType"/> at runtime.
        /// </summary>
        public enum AuthorityTransferConfig
        {
            /// <summary>
            /// Mapped into the DefaultAuthorityTransferType set in the coherence settings.
            /// </summary>
            Default = -1,

            /// <inheritdoc cref="AuthorityTransferType.NotTransferable"/>
            [InspectorName("Not transferable")] NotTransferable = 0,

            /// <inheritdoc cref="AuthorityTransferType.Request"/>
            Request = 1,

            /// <inheritdoc cref="AuthorityTransferType.Stealing"/>
            [InspectorName("Steal")] Stealing = 2,
        }

        /// <summary>
        /// What should happen to this entity when the client
        /// with state authority over it disconnects or abandons it.
        /// </summary>
        public enum LifetimeType
        {
            /// <summary>
            /// The entity will be destroyed on disconnection.
            /// </summary>
            SessionBased = 0,

            /// <summary>
            /// The entity will be kept alive, potentially to be adopted by
            /// another client or simulator.
            /// </summary>
            /// <seealso cref="OrphanedBehavior"/>
            /// <seealso cref="CoherenceSync.IsOrphaned"/>
            /// <seealso cref="CoherenceSync.EntityState"/>
            Persistent = 1,
        }

        /// <summary>
        /// Whether this entity should be auto-adopted on disconnects.
        /// </summary>
        /// <seealso cref="CoherenceSync.IsOrphaned"/>
        /// <seealso cref="LifetimeType.Persistent"/>
        public enum OrphanedBehavior
        {
            DoNothing = 0,
            AutoAdopt = 1,
        }

        /// <summary>
        /// Should this entity enforce that only a single instance with the same
        /// unique ID can exist at the same time.
        /// </summary>
        public enum UniquenessType
        {
            /// <summary>
            /// Multiple entities with the same unique ID value can coexist.
            /// </summary>
            AllowDuplicates = 0,

            /// <summary>
            /// Only a single entity with the same unique ID value can exist.
            /// Other instances will be destroyed.
            /// </summary>
            NoDuplicates = 1,
        }

        public enum UniqueObjectReplacementStrategy
        {
            [Tooltip("Should be used for objects that remain in the scene, will preserve local customizations.")]
            Replace = 0,
            [Tooltip("Should be used for objects that move to other scenes, will not preserve local customizations.")]
            Destroy = 1,
        }

        public enum UnsyncedNetworkEntityPriority
        {
            [Tooltip("This is the default option and coherence will use the Asset Id from the CoherenceSyncConfig to relate Remote Network Entities to instantiated objects of this Prefab.")]
            AssetId = 0,
            [Tooltip("With this option coherence will force instantiated objects of this Prefab to match the Unique Id of a Remote Network Entity that is not synchronized yet with a Unity Object.")]
            UniqueId = 1
        }

        /// <summary>
        /// How and where this entity is simulated.
        /// </summary>
        public enum SimulationType
        {
            ClientSide = 0,
            ServerSide = 2,
            ServerSideWithClientInput = 3
        }

        /// <summary>
        /// At what point in the Unity update loop should interpolation happen.
        /// </summary>
        [Flags]
        public enum InterpolationLoop
        {
            /// <summary>
            /// Interpolate in Unity's 'Update'.
            /// </summary>
            [InspectorName("Update")]
            Update = 1 << 0,

            /// <summary>
            /// Interpolate in Unity's 'LateUpdate'.
            /// </summary>
            [InspectorName("LateUpdate")]
            LateUpdate = 1 << 1,

            /// <summary>
            /// Interpolate in Unity's 'FixedUpdate'.
            /// </summary>
            [InspectorName("FixedUpdate")]
            FixedUpdate = 1 << 2,

            /// <summary>
            /// Interpolate in both Unity's 'Update' and 'FixedUpdate'.
            /// </summary>
            [InspectorName("Update and FixedUpdate")]
            UpdateAndFixedUpdate = Update | FixedUpdate,

            /// <summary>
            /// Interpolate in both Unity's 'LateUpdate' and 'FixedUpdate'.
            /// </summary>
            [InspectorName("LateUpdate and FixedUpdate")]
            LateUpdateAndFixedUpdate = LateUpdate | FixedUpdate,
        }

        /// <summary>
        /// Behaviour when updating this entity, when there's a <see cref="Rigidbody"/>.
        /// </summary>
        public enum RigidbodyMode
        {
            /// <summary>
            /// Direct is the default mode that sets the rigidbody position and rotation directly.
            /// Use this if you're parenting entities with and want replicated entities to have
            /// kinetic rigidbodies. Can be used with CoherenceNode and parenting.
            /// </summary>
            Direct,
            /// <summary>
            /// Interpolated uses the rigidbody MovePosition and MoveRotation calls to maintain
            /// the physical state of the replicated rigidbody. Use this if you need replicated entities
            /// to have <see cref="Rigidbody.velocity"/> and <see cref="Rigidbody.angularVelocity"/>
            /// replicated. Note: does not support using CoherenceNode or parenting since the coherence
            /// position and rotation are derived from the rigid body and so are always in world position.
            /// </summary>
            Interpolated,
            /// <summary>
            /// Manual calls a user defined callback which is passed world position
            /// or rotation for manual application on the rigid body.  Use this is you want manual
            /// control of the update of the state of the CoherenceSync's game object rigid body.
            /// Note: does not support using CoherenceNode or parenting since the coherence position
            /// and rotation are derived from the rigid body and so are always in world position.
            /// </summary>
            Manual,
        }

        /// <summary>The delegate type of <see cref="CoherenceSync.OnAuthorityRequested" />.</summary>
        /// <param name="requesterID">The ClientID of the requesting client.</param>
        /// <param name="sync">The entity that the authority request is targeting.</param>
        /// <returns>Return true to accept the request for authority.</returns>
        public delegate bool OnAuthorityRequestedHandler(ClientID requesterID, AuthorityType authorityType, CoherenceSync sync);

        /// <summary>
        /// Invoked when a client requests authority over this entity.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="AuthorityTransferType"/> set to <see cref="AuthorityTransferType.Request"/>.
        /// </remarks>
        /// <seealso cref="AuthorityTransferType.Request"/>
        /// <seealso cref="OnAuthorityRequest"/>
        [Obsolete("Use " + nameof(OnAuthorityRequest) + " instead.")]
        [Deprecated("05/2025", 1, 7, 0, Reason = "Use " + nameof(OnAuthorityRequest) + " instead.")]
        public event OnAuthorityRequestedHandler OnAuthorityRequested;

        /// <summary>
        /// Invoked when a client requests authority over this entity.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="AuthorityTransferType"/> set to <see cref="AuthorityTransferType.Request"/>.
        /// </remarks>
        /// <seealso cref="AuthorityTransferType.Request"/>
        public UnityEventCountable<AuthorityRequest, CoherenceSync> OnAuthorityRequest;

        event UnityAction ICoherenceSync.OnStateRemote
        {
            add => OnStateRemote.AddListener(value);
            remove => OnStateRemote.RemoveListener(value);
        }

        event UnityAction ICoherenceSync.OnStateAuthority
        {
            add => OnStateAuthority.AddListener(value);
            remove => OnStateAuthority.RemoveListener(value);
        }

        event UnityAction ICoherenceSync.OnInputAuthority
        {
            add => OnInputAuthority.AddListener(value);
            remove => OnInputAuthority.RemoveListener(value);
        }

        /// <summary>
        /// Event triggered when this CoherenceSync is instantiated and seen on the network
        /// </summary>
        /// <remarks>
        /// When the event is triggered, authority should be already resolved.
        /// </remarks>
        [FormerlySerializedAs("onNetworkedInstantiation")]
        public UnityEvent<CoherenceSync> OnNetworkedInstantiation;

        /// <summary>
        /// Event triggered before this CoherenceSync is instantiated and seen on the network
        /// </summary>
        /// <remarks>
        /// When the event is triggered, authority is not yet resolved.
        /// </remarks>
        [FormerlySerializedAs("onBeforeNetworkedInstantiation")]
        public UnityEvent<CoherenceSync> OnBeforeNetworkedInstantiation;

        /// <summary>
        /// Event triggered before this CoherenceSync is destroyed by the network.
        /// </summary>
        [FormerlySerializedAs("onNetworkedDestruction")]
        public UnityEvent<CoherenceSync> OnNetworkedDestruction;

        /// <summary>
        /// Called when the entity loses state authority.
        /// </summary>
        [FormerlySerializedAs("OnStateAuthorityLost")]
        public UnityEvent OnStateRemote;

        /// <summary>
        /// Called when the entity loses input authority.
        /// </summary>
        /// <remarks>
        /// Can happen when another entity successfully requests or steals authority.
        /// </remarks>
        [FormerlySerializedAs("OnInputAuthorityLost")]
        public UnityEvent OnInputRemote;

        /// <summary>
        /// Called when the entity gains state authority.
        /// </summary>
        [FormerlySerializedAs("OnStateAuthorityGained")]
        public UnityEvent OnStateAuthority;

        /// <summary>
        /// Called when the entity gains input authority.
        /// </summary>
        [FormerlySerializedAs("OnInputAuthorityGained")]
        public UnityEvent OnInputAuthority;

        /// <summary>
        /// Called when authority transfer was requested and rejected.
        /// </summary>
        public UnityEvent<AuthorityType> OnAuthorityRequestRejected;

        /// <summary>
        /// Called when the authority transfer or abandonment of this entity is completed.
        /// </summary>
        public UnityEvent OnAuthTransferComplete;

        /// <inheritdoc cref="OnNetworkedParentChanged"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use " + nameof(OnNetworkedParentChanged) + " instead.")]
        [Deprecated("05/2025", 1, 7, 0, Reason = "Use " + nameof(OnNetworkedParentChanged) + " instead.")]
        public UnityEvent<CoherenceSync> OnConnectedEntityChanged
        {
            get => OnNetworkedParentChanged;
            set => OnNetworkedParentChanged = value;
        }

        /// <summary>
        /// Called when the parent of this entity changed.
        /// </summary>
        /// <remarks>
        /// Only called when you don't have authority over the entity.
        /// </remarks>
        [FormerlySerializedAs("OnConnectedEntityChanged")]
        public UnityEvent<CoherenceSync> OnNetworkedParentChanged;

        /// <summary>
        /// Called when a simulator is connected and ready to handle input state.
        /// </summary>
        public UnityEvent OnInputSimulatorConnected;

        /// <summary>
        /// Called when the <see cref="Rigidbody2D"/> position is updated.
        /// </summary>
        /// <remarks>
        /// Called only if <see cref="CoherenceSync.RigidbodyUpdateMode" /> is set to <see cref="RigidbodyMode.Manual"/>.
        /// </remarks>
        public UnityEvent<Vector2> OnRigidbody2DPositionUpdate;
        /// <summary>
        /// Called when the <see cref="Rigidbody"/> position is updated.
        /// </summary>
        /// <remarks>
        /// Called only if <see cref="CoherenceSync.RigidbodyUpdateMode" /> is set to <see cref="RigidbodyMode.Manual"/>.
        /// </remarks>
        public UnityEvent<Vector3> OnRigidbody3DPositionUpdate;
        /// <summary>
        /// Called when the <see cref="Rigidbody2D"/> rotation is updated.
        /// </summary>
        /// <remarks>
        /// Called only if <see cref="CoherenceSync.RigidbodyUpdateMode" /> is set to <see cref="RigidbodyMode.Manual"/>.
        /// </remarks>
        public UnityEvent<float> OnRigidbody2DRotationUpdate;
        /// <summary>
        /// Called when the <see cref="Rigidbody"/> rotation is updated.
        /// </summary>
        /// <remarks>
        /// Called only if <see cref="CoherenceSync.RigidbodyUpdateMode" /> is set to <see cref="RigidbodyMode.Manual"/>.
        /// </remarks>
        public UnityEvent<Quaternion> OnRigidbody3DRotationUpdate;

        UnityEvent ICoherenceSync.OnInputSimulatorConnected => OnInputSimulatorConnected;

        internal Logger logger
        {
            get => loggerBacking ??= Log.GetLogger<CoherenceSync>();
            set => loggerBacking = value;
        }
        private Logger loggerBacking;

        /// <summary>
        /// The delegate type of <see cref="ConnectedEntityChangeOverride"/>.
        /// </summary>
        /// <param name="newConnectedEntity">The `CoherenceSync` of the new connected entity.</param>
        public delegate void ConnectedEntityChangeHandler(CoherenceSync newConnectedEntity);

        /// <summary>
        /// The delegate type of <see cref="ConnectedEntityChangeOverride"/>.
        /// </summary>
        /// <param name="newConnectedEntity">The `CoherenceSync` of the new connected entity.</param>
        public delegate void ConnectedEntitySentHandler(CoherenceSync newConnectedEntity);

        /// <summary>
        /// Used to override the default parenting behaviour.
        /// </summary>
        /// <remarks>
        /// It can be used to implement more advanced hierarchies than direct parent-to-child relationships.
        /// </remarks>
        /// <seealso cref="Coherence.Toolkit.CoherenceNode"/>
        public event ConnectedEntityChangeHandler ConnectedEntityChangeOverride;

        /// <summary>
        /// Called after the entity has changed its parent and sent this update to other clients.
        /// </summary>
        /// <remarks>
        /// Listening to this event doesn't change any behaviour,
        /// but can be useful for reacting to changes in the connected entity for a simulated entity.
        /// </remarks>
        public event ConnectedEntitySentHandler DidSendConnectedEntity;

        internal delegate void NetworkCommandHandler(object sender, byte[] data);

        internal NetworkCommandHandler NetworkCommandReceived;

        /// <summary>
        /// The config that's linked to this entity.
        /// </summary>
        /// <seealso cref="CoherenceSyncConfigRegistry"/>
        public CoherenceSyncConfig CoherenceSyncConfig { get => coherenceSyncConfig; internal set => coherenceSyncConfig = value; }

        [SerializeField] private CoherenceSyncConfig coherenceSyncConfig;
        [SerializeField] private ToolkitArchetype archetype = new();

        /// <summary>
        /// Contains all information related to LODing, which can be set up
        /// in the 'Optimize' window of the CoherenceSync inspector.
        ///
        /// This property is set to null if the entity isn't using LODs.
        /// </summary>
        internal ToolkitArchetype Archetype => archetype;

        private CoherenceInput input;

        /// <summary>
        /// Input component on this entity, if any.
        /// </summary>
        public CoherenceInput Input => input;

        /// <summary>
        /// <see langword="true"/> if the entity has a <see cref="Coherence.Toolkit.CoherenceInput"/> component.
        /// </summary>
        /// <remarks>
        /// This will only return a valid response at runtime. In edit mode it will always return <see langword="false" />.
        /// </remarks>
        public bool HasInput => input != null;

        [FormerlySerializedAs("InterpolationLocation")]
        [SerializeField]
        private InterpolationLoop interpolationLocation = InterpolationLoop.Update;

        /// <summary>
        /// Determines the loop in which the interpolation will be performed.
        /// </summary>
        /// <remarks>
        /// If there is a rigidbody, <see cref="FixedUpdate"/> will always be used in addition to other loops
        /// from this setting.
        /// </remarks>
        public InterpolationLoop InterpolationLocationConfig
        {
            get => interpolationLocation;
            set
            {
                if (bridge != null)
                {
                    bridge.EntitiesManager.UpdateInterpolationLoopConfig(this, value);
                }

                interpolationLocation = value;
            }
        }

        /// <summary>
        /// Determines how the properties of an attached rigidbody are updated.
        /// </summary>
        /// <remarks>
        /// If there is an attached rigidbody, and the mode is <see cref="RigidbodyMode.Interpolated"/>,
        /// then the <see cref="InterpolationLocationConfig" /> must be set to only <see cref="FixedUpdate"/>
        /// or there will be inconsistencies in the state of the rigidbody and the values of the
        /// angular and linear velocity are lost.
        /// </remarks>
        public RigidbodyMode RigidbodyUpdateMode = RigidbodyMode.Direct;

        ICoherenceSyncUpdater ICoherenceSync.Updater => updater;

        /// <summary>
        /// NOTE: This field is accessed via reflection in UnityTestExtensions.SetUpdated.
        /// This should not be renamed nor its type changed without also updating that method accordingly.
        /// </summary>
        private ICoherenceSyncUpdater updater;
        private Rigidbody syncRigidbody;
        private Rigidbody2D syncRigidbody2D;
        private bool hasPhysics => syncRigidbody != null || syncRigidbody2D != null;
        private bool resettingInterpolation;

        private ICoherenceBridge bridge;
        private UniquenessManager uniquenessManager;

        [SerializeField] private string bakedScriptType;

        CoherenceSyncBaked ICoherenceSync.BakedScript => bakedScript;
        private CoherenceSyncBaked bakedScript;

        /// <summary>
        ///     The closest parent entity (with `CoherenceSync`) above this entity
        ///     in the transform hierarchy.
        ///
        ///     Please note that this is not always the direct parent of this entity's
        ///     transform, e.g. when using <see cref="Coherence.Toolkit.CoherenceNode"/>.
        /// </summary>
        /// <return>
        ///     Will return null if there is no connected entity.
        /// </return>
        public CoherenceSync ConnectedEntity { get; private set; }
        private Transform lastSentParent;
        private Vector3 lastSentRelativePosition;
        private Quaternion lastSentRelativeRotation;
        private Vector3 lastSentRelativeScale;
        private Transform lastReceivedParent;
        private Transform lastValidatedParent;
        private bool lastValidatedParentSet;
        private Transform tform;

        /// <summary>
        /// Holds information for the state of the entity.
        /// </summary>
        public NetworkEntityState EntityState { get; internal set; }

        /// <summary>
        /// <see langword="true"/> when <see cref="uniquenessType"/> is <see cref="UniquenessType.NoDuplicates"/>.
        /// </summary>
        public bool IsUnique => uniquenessType == UniquenessType.NoDuplicates;

        /// <summary>
        /// When set this entity is visible to global queries.
        /// </summary>
        public bool IsGlobal => isGlobal;

        [SerializeField]
        private bool isGlobal;

        /// <summary>
        /// How uniqueness is resolved when multiple instances of the entity exists.
        /// </summary>
        public UniqueObjectReplacementStrategy ReplacementStrategy => replacementStrategy;

        /// <summary>
        /// When resolving uniqueness, whether the asset ID or the uniqueness ID should be taken into account.
        /// </summary>
        public UnsyncedNetworkEntityPriority UnsyncedEntityPriority
        {
            get => unsyncedNetworkEntityPriority;
            set => unsyncedNetworkEntityPriority = value;
        }

        SimulationType ICoherenceSync.SimulationTypeConfig => simulationType;
        LifetimeType ICoherenceSync.LifetimeTypeConfig => lifetimeType;
        AuthorityTransferType ICoherenceSync.AuthorityTransferTypeConfig => AuthorityTransfer;
        bool ICoherenceSync.PreserveChildren => preserveChildren;
        string ICoherenceSync.ArchetypeName => "_" + CoherenceSyncConfig.ID;

        /// <summary>
        /// Fallback value used when <see cref="simulationType"/> is set
        /// to <see cref="AuthorityTransferType.Request"/> and <see cref="OnAuthorityRequested"/>
        /// is unsubscribed to.
        /// </summary>
        public bool approveAuthorityTransferRequests = true;

        /// <summary>
        /// How and where this entity is simulated.
        /// </summary>
        // fixme: want to inherit doc from enum
        [Tooltip("Define how and where this entity is simulated.\n\nClient Side: State and input authority are kept by" +
                 " the client that instantiates this GameObject, until authority is transferred to a different client." +
                 "\n\nServer Side: State and input authority are transferred to a Simulator. If a client instantiates this " +
                 "GameObject, a transfer request to the Simulator is performed. If a Simulator instantiates this GameObject, " +
                 "nothing happens.\n\nServer Side With Client Input: State authority is transferred to a Simulator, " +
                 "but input authority is kept by the client that instantiates this GameObject")]
        public SimulationType simulationType;

        [Obsolete("Use AuthorityTransfer instead.")]
        [Deprecated("29/04/2025", 1, 7, 0)]
        public AuthorityTransferType authorityTransferType
        {
            get => AuthorityTransfer;
            set => AuthorityTransfer = value;
        }

        /// <summary>
        /// How this entity should respond to requests for authority
        /// (via <see cref="RequestAuthority(Coherence.AuthorityType)"/>) by other clients.
        /// </summary>
        // fixme: want to inherit doc from enum
        public AuthorityTransferType AuthorityTransfer
        {
            get => authorityTransferConfig.MapToType();
            set => authorityTransferConfig = value.MapToConfig();
        }

        /// <summary>
        /// Configuration of <see cref="AuthorityTransfer"/>.
        /// </summary>
        // fixme: want to inherit doc from enum
        [SerializeField, FormerlySerializedAs("authorityTransferType")]
        [Tooltip("Define how this Entity should respond to requests for authority by other clients.\n\nNot Transferable: " +
                 "All transfer requests are rejected automatically.\n\nRequest: Authority transfer may be requested by any client. " +
                 "The current owner decides if the transfer is accepted or not.\n\nSteal: Authority will always be given to the requesting client on a FCFS (\"First Come First Serve\") basis.")]
        public AuthorityTransferConfig authorityTransferConfig = AuthorityTransferConfig.Default;

        /// <summary>
        /// How this entity should behave if it is abandoned by its client.
        /// </summary>
        /// <remarks>
        /// An example of an entity becoming orphaned is when a disconnect occurs, and the entity is <see cref="LifetimeType.Persistent"/>.
        /// </remarks>
        // fixme: want to inherit doc from enum
        public OrphanedBehavior orphanedBehavior;

        OrphanedBehavior ICoherenceSync.OrphanedBehaviorConfig => orphanedBehavior;

        /// <summary>
        ///     What should happen to this entity after the client
        ///     with authority over it disconnects or abandons it.
        /// </summary>
        // fixme: want to inherit doc from enum
        [Tooltip("Define what should happen to this Entity after the client with authority over it disconnects or abandons it.\n\n" +
                 "Session Based: The Entity will be destroyed when the Client or Simulator that has authority over it disconnects.\n\n" +
                 "Persistent: The Entity will remain on the Replication Server until a simulating Client or Simulator destroys it explicitly.")]
        public LifetimeType lifetimeType;

        /// <summary>
        /// Determines if it should be enforced that only a single instance with the same
        /// unique can exist at the same time on the network.
        /// </summary>
        /// <seealso cref="ManualUniqueId"/>
        /// <seealso cref="uniquenessManager"/>
        // fixme: want to inherit doc from enum
        [Tooltip("Define if this Entity enforces that only a single instance with the same UUID can exist at the same time.\n\n" +
                 "Allow Duplicates: Every instance of this prefab will create a new Network Entity.\n\n" +
                 "No Duplicates: Instances of this prefab that share the same UUID cannot be duplicated. Duplicated instances will be destroyed upon creation.")]
        public UniquenessType uniquenessType;

        [SerializeField]
        private UniqueObjectReplacementStrategy replacementStrategy;
        [SerializeField]
        private UnsyncedNetworkEntityPriority unsyncedNetworkEntityPriority;

        /// <summary>
        /// When this entity is destroyed, are the connected coherence sync children also destroyed?
        /// By default, child entities are destroyed.
        ///
        /// For internal use only, do not change in code.
        /// </summary>
        // fixme: Should be internal, blocked by ImplSync.gen.cs
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool preserveChildren;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Bindings instead.")]
        [Deprecated("17/9/2024", 1, 3, 0)]
        [SerializeReference] public List<Binding> bindings = new();

        /// <summary>
        /// List of variables and methods that are synchronized over the network.
        /// </summary>
#pragma warning disable CS0618
        public List<Binding> Bindings => bindings;
#pragma warning restore CS0618

        /// <summary>
        /// A unique identifier used to ensure that CoherenceSync prefabs that are instanced and serialized in the scene with <see cref="uniquenessType"/>
        /// set to <see cref="UniquenessType.NoDuplicates"/> only have a single instance
        /// across all clients, if not set, the UUID will be autogenerated.
        /// </summary>
        [SerializeField, HideInInspector] private string scenePrefabInstanceUUID;
        /// <summary>
        /// A unique identifier used to ensure that CoherenceSync prefabs with <see cref="uniquenessType"/>
        /// set to <see cref="UniquenessType.NoDuplicates"/> only have a single instance
        /// across all clients, if not set, the UUID will be autogenerated.
        /// </summary>
        [SerializeField] private string coherenceUUID;

        private string uuidToBeDeRegistered;

        /// <summary>
        /// The unique identifier of this entity.
        /// </summary>
        /// <remarks>
        /// Used to ensure that CoherenceSync prefabs with <see cref="uniquenessType"/>
        /// set to <see cref="UniquenessType.NoDuplicates"/> only have a single instance
        /// across all clients, if not set, the UUID will be autogenerated.
        ///
        /// You cannot change an instance unique id at runtime.
        /// If you wish to use runtime-generated unique ids, register them through
        /// <see cref="UniquenessManager.RegisterUniqueId"/> instead.
        /// </remarks>
        public string ManualUniqueId { get => coherenceUUID; internal set => coherenceUUID = value; }

        /// <inheritdoc cref="UniquenessManager.RegisterUniqueId"/>
        /// <seealso cref="UniquenessManager"/>
        public void RegisterUniqueId(string uniqueIdentifier)
        {
            CoherenceBridge.UniquenessManager.RegisterUniqueId(uniqueIdentifier);
        }

        [SerializeField] private string assetVersion;

        /// <summary>
        /// <see langword="true"/> if the lifetime is <see cref="LifetimeType.Persistent"/>.
        /// </summary>
        public bool IsPersistent => lifetimeType == LifetimeType.Persistent;

        /// <summary>
        /// <see langword="true"/> if your client has state authority over the entity.
        /// </summary>
        /// <remarks>
        /// State authority is an actual owner of the entity and controls all the synced variables.
        /// </remarks>
        public bool HasStateAuthority => EntityState?.AuthorityType.Value.ControlsState() ?? true;
        /// <summary>
        /// <see langword="true"/> if your client has input authority over the entity.
        /// </summary>
        /// <remarks>
        /// Input authority can produce inputs via <see cref="CoherenceInput"/> which are then sent
        /// to the state authority who processes them.
        /// </remarks>
        public bool HasInputAuthority => EntityState?.AuthorityType.Value.ControlsInput() ?? true;
        /// <summary>
        /// <see langword="true"/> if the network acknowledges this entity (it's synchronized).
        /// </summary>
        public bool IsSynchronizedWithNetwork => EntityState != null;

        /// <summary>
        /// <see langword="true"/> when no client has authority over the entity.
        /// </summary>
        /// <seealso cref="EntityState"/>
        public bool IsOrphaned => EntityState?.IsOrphaned ?? false;

        [SerializeField, CoherenceTag]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use CoherenceTag instead.")]
        [Deprecated("17/9/2024", 1, 3, 0)]
        public string coherenceTag = "";

        /// <summary>
        /// The tag used to filter by via <see cref="CoherenceTagQuery"/>.
        /// </summary>
#pragma warning disable CS0618
        public string CoherenceTag { get => coherenceTag; set => coherenceTag = value; }
#pragma warning restore CS0618

        internal IClient ClientInternal { get; set; }

        /// <summary>
        /// The <see cref="CoherenceBridge"/> that this entity is connecting through.
        /// </summary>
        public CoherenceBridge CoherenceBridge => (CoherenceBridge)bridge;

        ICoherenceBridge ICoherenceSync.CoherenceBridge => bridge;

        /// <summary>
        /// Used to resolve which <see cref="CoherenceBridge"/> should be associated with this entity.
        /// </summary>
        /// <remarks>
        /// On complex scenarios where a bridge cannot be easily inferred, this helps explicitly point out to the bridge
        /// that should handle the connection.
        /// </remarks>
        public static event CoherenceBridgeResolver<CoherenceSync> BridgeResolve;

        private CommandsHandler commandsHandler;

        [SerializeReference] internal ComponentAction[] componentActions;

        private bool destroyAsDuplicate = false;

        public enum FloatingOriginMode
        {
            /// <summary>
            /// Object uses absolute position and will stay behind when Floating Origin moves.
            /// </summary>
            DontMoveWithFloatingOrigin = 0,

            /// <summary>
            /// Object uses relative position and will move together with Floating Origin.
            /// </summary>
            MoveWithFloatingOrigin = 1,
        }

        /// <summary>
        /// Called when the floating origin has changed position.
        /// </summary>
        public Action<Vector3, Vector3> OnFloatingOriginShifted { get; set; }

        /// <summary>
        /// Determines how the floating origin behaves when the entity is not parented.
        /// </summary>
        /// <seealso cref="floatingOriginParentedMode"/>
        public FloatingOriginMode floatingOriginMode = FloatingOriginMode.DontMoveWithFloatingOrigin;

        /// <summary>
        /// Determines how the floating origin behaves when the entity is parented.
        /// </summary>
        /// <seealso cref="floatingOriginMode"/>
        public FloatingOriginMode floatingOriginParentedMode = FloatingOriginMode.MoveWithFloatingOrigin;

        [NonSerialized] private PositionBinding positionBinding;
        internal PositionBinding PositionBinding => positionBinding ??= (PositionBinding)Bindings.FirstOrDefault(b => b is PositionBinding && b.unityComponent == transform);

        [NonSerialized] private RotationBinding rotationBinding;
        internal RotationBinding RotationBinding => rotationBinding ??= (RotationBinding)Bindings.FirstOrDefault(b => b is RotationBinding && b.unityComponent == transform);

        [NonSerialized] private ScaleBinding scaleBinding;
        internal ScaleBinding ScaleBinding => scaleBinding ??= (ScaleBinding)Bindings.FirstOrDefault(b => b is ScaleBinding && b.unityComponent == transform);

        private bool bakedBindings;

        private CoherenceNode coherenceNode;

        void ICoherenceSync.OnNetworkCommandReceived(object sender, byte[] data)
        {
            NetworkCommandReceived?.Invoke(sender, data);
        }

        private bool UpdateBakedScriptReference()
        {
            if (bakedScript != null)
            {
                return true;
            }

            if (string.IsNullOrEmpty(bakedScriptType))
            {
                logger.Warning(Warning.ToolkitSyncBakedScriptNoTypeStored,
                    $"Couldn't load baked script for '{name}'.\nReimport the prefab.",
                    ("context", this));
                return false;
            }

            var t = Type.GetType(bakedScriptType);
            if (t == null)
            {
                logger.Warning(Warning.ToolkitSyncBakedScriptMissingType,
                $"Couldn't load baked script for '{name}'.\n" +
                    $"Check the {nameof(CoherenceSync)} Inspector for possible troubleshooting.\n" +
                    $"Type not found: {bakedScriptType}",
                    ("context", this),
                    ("bakedScriptType", bakedScriptType));
                return false;
            }

            if (commandsHandler == null)
            {
                // The commandsHandler may not have been initialized when called from SpawnInfo.GetBindingValue on a prefab
                commandsHandler = new CommandsHandler(this, Bindings, logger);
            }

            try
            {
                bakedScript = (CoherenceSyncBaked)Activator.CreateInstance(t);
            }
            catch (Exception e)
            {
                logger.Error(Error.ToolkitSyncInstantiationException,
                    $"Can't create baked script instance for {name}. " +
                        $"If you've made changes to this Prefab, make sure Bake is up-to-date.\n\n" +
                        $"{e.Message} {e.StackTrace}");
            }
            finally
            {
                if (bakedScript == null)
                {
                    logger.Warning(Warning.ToolkitSyncBakedScriptFailedToLoad,
                        ("prefab", name));
                }
            }

            return bakedScript != null;
        }

        private void BakeBindings()
        {
            for (var i = 0; i < Bindings.Count; i++)
            {
                var binding = Bindings[i];

                if (binding is CommandBinding commandBinding)
                {
                    bakedScript.BakeCommandBinding(commandBinding, commandsHandler);
                }
                else
                {
                    var bakedBinding = bakedScript.BakeValueBinding(binding);

                    if (bakedBinding != null)
                    {
                        Bindings[i] = bakedBinding;
                    }
                    else
                    {
                        logger.Error(Error.ToolkitBindingMissing, $"No Baked Binding '{binding.FullName}' for '{name}'.\nCheck CoherenceSync inspector, Configure window or Networked Prefab window to troubleshoot.");
                    }
                }
            }

            bakedBindings = true;
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void Reset()
        {
            base.Reset();
            bakedScript = null;
        }

        private void Awake()
        {
            if (!IsIncludedInSchema())
            {
                return;
            }

#if UNITY_EDITOR
            positionBinding = null;
            scaleBinding = null;
            rotationBinding = null;
#endif

            ResolveUniqueId();

            coherenceNode = GetComponent<CoherenceNode>();

            commandsHandler = new CommandsHandler(this, Bindings, logger);

            // is replaced by monobridge logger later.
            logger = Log.GetLogger<CoherenceSync>().WithArgs(("context", this));

            tform = transform;
            _ = TryGetComponent(out input);
            _ = TryGetComponent(out syncRigidbody);
            _ = TryGetComponent(out syncRigidbody2D);

            if (UpdateBakedScriptReference())
            {
                BakeBindings();
            }
        }

        private bool IsIncludedInSchema()
        {
            return coherenceSyncConfig == null || coherenceSyncConfig.IncludeInSchema;
        }

        private void ResolveUniqueId()
        {
            coherenceUUID = !string.IsNullOrEmpty(coherenceUUID) ? coherenceUUID : scenePrefabInstanceUUID;
        }

        private void ResolveUniqueIdFromUniquenessManager()
        {
            if (!IsUnique)
            {
                return;
            }

            var uuid = uniquenessManager.GetUniqueId();
            coherenceUUID = !string.IsNullOrEmpty(uuid) ? uuid : coherenceUUID;

            if (string.IsNullOrEmpty(coherenceUUID))
            {
                coherenceUUID = Guid.NewGuid().ToString();
            }
        }

        private void OnEnable()
        {
            if (IsClientConnectionInstance())
            {
                return;
            }

            if (!IsIncludedInSchema())
            {
                return;
            }

            ActivateBindings();
            _ = ConnectToBridge();

            if (bridge != null && bridge.IsConnected)
            {
                HandleConnected(bridge);

                if (!gameObject.activeSelf)
                {
                    return;
                }
            }

            OnBecomesAuthority(HasStateAuthority, HasInputAuthority);
        }

        private bool IsClientConnectionInstance()
        {
            if (EntityState != null && EntityState.ClientConnection != null)
            {
                return true;
            }

            return false;
        }

        private void ValidateSyncProperties()
        {
            if (EntityState?.IsMyClientConnection ?? false)
            {
                if (authorityTransferConfig != AuthorityTransferConfig.NotTransferable)
                {
                    // It might be Default, and the default value might be NotTransferable. Don't warn in that case.
                    if (AuthorityTransfer != AuthorityTransferType.NotTransferable)
                    {
                        logger.Warning(Warning.ToolkitSyncValidateTransferType,
                            $"Client connection prefabs should not be {authorityTransferConfig}, forcing to {nameof(AuthorityTransferConfig.NotTransferable)}",
                            ("name", name),
                            ("scene", gameObject.scene.IsValid() ? gameObject.scene.name : "None"));
                    }

                    authorityTransferConfig = AuthorityTransferConfig.NotTransferable;
                }

                if (simulationType == SimulationType.ServerSideWithClientInput)
                {
                    logger.Error(Error.ToolkitSyncValidateSimulationType,
                        $"Client connection prefabs should not be {simulationType}",
                        ("name", name),
                        ("scene", gameObject.scene.IsValid() ? gameObject.scene.name : "None"));
                }

                if (lifetimeType == LifetimeType.Persistent)
                {
                    logger.Warning(Warning.ToolkitSyncValidateLifetimeType,
                        $"Client connection prefabs should not be {lifetimeType}, forcing to {nameof(LifetimeType.SessionBased)}",
                        ("name", name),
                        ("scene", gameObject.scene.IsValid() ? gameObject.scene.name : "None"));

                    lifetimeType = LifetimeType.SessionBased;
                }
            }
            else
            {
                if (AuthorityTransfer == AuthorityTransferType.NotTransferable && simulationType == SimulationType.ServerSideWithClientInput)
                {
                    // force to stealing, since no transfer is not possible in this mode.
                    logger.Warning(Warning.ToolkitSyncValidateTransferType,
                        $"{nameof(AuthorityTransferType.NotTransferable)} is not supported in {nameof(SimulationType.ServerSideWithClientInput)} mode. Forcing to {nameof(AuthorityTransferType.Stealing)}");

                    AuthorityTransfer = AuthorityTransferType.Stealing;
                }
                else if (AuthorityTransfer == AuthorityTransferType.NotTransferable && lifetimeType == LifetimeType.Persistent)
                {
                    logger.Warning(Warning.ToolkitSyncValidateTransferType,
                        $"{nameof(AuthorityTransferType.NotTransferable)} is not supported when lifetime is {nameof(LifetimeType.Persistent)}. Forcing to {nameof(AuthorityTransferType.Stealing)}");

                    AuthorityTransfer = AuthorityTransferType.Stealing;
                }
            }
        }

        private bool ConnectToBridge()
        {
            if (updater != null)
            {
                updater.TaggedForNetworkedDestruction = false;
            }

            if (bridge != null)
            {
                return false;
            }

            if (BridgeResolve != null)
            {
                bridge = BridgeResolve(this);
            }
            else if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out var coherenceBridge))
            {
                enabled = false;
                return false;
            }
            else
            {
                bridge = coherenceBridge;
            }

            ClientInternal = bridge.Client;
            bridge.OnConnectedInternal += HandleConnected;
            updater ??= new CoherenceSyncUpdater(this, ClientInternal);
            uniquenessManager = bridge.UniquenessManager;

            ResolveUniqueIdFromUniquenessManager();

            logger = bridge.Logger.With<CoherenceSync>().WithArgs(("context", this));
            commandsHandler.logger = logger;
            updater.logger = logger;
            return true;
        }

        private void ResetBridge()
        {
            if (bridge != null)
            {
                bridge.OnConnectedInternal -= HandleConnected;
                bridge = null;
            }
        }

        private void HandleConnected(ICoherenceBridge coherenceBridge)
        {
            if (bridge == null || bridge != coherenceBridge)
            {
                var message = bridge == null ? "null" : "mismatched";
                logger.Error(Error.ToolkitSyncInvalidBridge,
                    $"Local bridge is {message} for {name}");
            }

            var currentBridge = bridge ?? coherenceBridge;
            if (currentBridge is null)
            {
                return;
            }

            (NetworkEntityState entityState, ComponentUpdates? entityData, uint? LOD, bool isServerObjectOnClient) resolvedEntity = currentBridge.EntitiesManager.SyncNetworkEntityState(this);

            if (resolvedEntity.isServerObjectOnClient)
            {
                gameObject.SetActive(false);
                return;
            }

            if (resolvedEntity.entityState == null)
            {
                return;
            }

            ClearEntityState();
            EntityState = resolvedEntity.entityState;
            EntityState.AuthorityType.OnValueUpdated += OnAuthorityChanged;

            if (!EntityState.CoherenceUUID.Equals(ManualUniqueId))
            {
                coherenceUUID = EntityState.CoherenceUUID;
            }

            if (EntityState.ClientConnection != null)
            {
                DontDestroyOnLoad(gameObject);
            }

            ValidateSyncProperties();

            ConstructGameObjectName();

            OnBeforeNetworkedInstantiation?.Invoke(this);

            bakedScript.Initialize(EntityState.EntityID, (CoherenceBridge)currentBridge, currentBridge.Client, Input, logger);

            if (resolvedEntity.entityData.HasValue)
            {
                updater.ApplyComponentUpdates(resolvedEntity.entityData.Value);

                if (resolvedEntity.LOD.HasValue && UsesLODsAtRuntime)
                {
                    ((ICoherenceSync)this).SetObservedLodLevel((int)resolvedEntity.LOD.Value);
                }

                currentBridge.EntitiesManager.ApplyDelayedUpdates(EntityState.EntityID);

                currentBridge.EntitiesManager.ApplyDelayedCommands(EntityState.EntityID);

                // Applying updates does nothing in itself, need to interpolate
                updater.PerformInterpolationOnAllBindings();
            }

            if (lastReceivedParent == null && transform.parent != null)
            {
                lastReceivedParent = transform.parent;
            }

            OnNetworkedInstantiation?.Invoke(this);

            OnBecomesAuthority(HasStateAuthority, HasInputAuthority);
            OnBecomesRemote(!HasStateAuthority, !HasInputAuthority);
        }

        private void ConstructGameObjectName()
        {
            if (bridge == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(bridge.NetworkPrefix))
            {
                return;
            }

            if (!HasStateAuthority && gameObject.name.Contains(bridge.NetworkPrefix))
            {
                return;
            }

            gameObject.name = (!HasStateAuthority && !gameObject.name.Contains(bridge.NetworkPrefix))
                ? $"{bridge.NetworkPrefix}{gameObject.name}"
                : gameObject.name.Replace(bridge.NetworkPrefix, string.Empty);
        }

        void ICoherenceSync.HandleDisconnected()
        {
            ClearEntityState();
            ResetBindings();
            ResetLastSentParent();
            ResetConnectedEntity();
        }

        void ICoherenceSync.InitializeReplacedUniqueObject(SpawnInfo info)
        {
            tform.position = info.position;
            tform.rotation = info.rotation ?? tform.rotation;

            UpdateChildrenOfReplacedUniqueObject();

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);

                if (CoherenceSyncConfig != null)
                {
                    CoherenceSyncConfig.Instantiator.OnUniqueObjectReplaced(this);
                }
            }
            else
            {
                if (destroyRoutine != null)
                {
                    StopCoroutine(destroyRoutine);
                    destroyRoutine = null;
                }

                // Adding this since it's possible that it was not cleared
                // in OnEnable since the bridge might already exist.
                updater.TaggedForNetworkedDestruction = false;

                OnEnable();
            }
        }

        bool ICoherenceSync.IsChildFromSyncGroup()
        {
            var group = GetComponentInParent<PrefabSyncGroup>(true);
            return group != null && group.gameObject != gameObject;
        }

        private void UpdateChildrenOfReplacedUniqueObject()
        {
            // Go through the all the children and get them to resend their connection component.
            // so that the changes are replicated.
            var childSyncs = GetComponentsInChildren<CoherenceSync>();
            foreach (var sync in childSyncs)
            {
                if (sync.ConnectedEntity == this)
                {
                    logger.Debug($"resetting the connection of child {sync.EntityState.EntityID} because duplicate parent was replaced.");

                    sync.ResetLastSentParent();
                }
            }
        }

        private bool GeneratesArchetypeDefinition => archetype?.GeneratesArchetypeDefinition ?? false;

        /// <summary>
        /// <see langword="true"/> if the entity use LODs at runtime.
        /// </summary>
        public bool UsesLODsAtRuntime => GeneratesArchetypeDefinition;

        void ICoherenceSync.SetObservedLodLevel(int lod)
        {
            if (!UsesLODsAtRuntime)
            {
                logger.Warning(Warning.ToolkitSyncSetLODNoLODSupport,
                    $"The method '{nameof(ICoherenceSync.SetObservedLodLevel)}' was called, but this entity doesn't use LODs.",
                    ("LOD", lod));

                return;
            }

            archetype?.SetObservedLodLevel(lod);
        }

        void ICoherenceSync.DestroyAsDuplicate()
        {
            destroyAsDuplicate = true;
            Destroy(gameObject);
        }

        private bool networkInstantiated;
        internal bool loadedViaCoherenceSyncConfig;

        internal void OnDestroy()
        {
            if (uniquenessManager != null && !destroyAsDuplicate && !string.IsNullOrEmpty(uuidToBeDeRegistered))
            {
                uniquenessManager.OnUniqueObjectDestroyed(uuidToBeDeRegistered);
                uuidToBeDeRegistered = string.Empty;
            }

            DestroyNetworkEntity();

            if (networkInstantiated || loadedViaCoherenceSyncConfig)
            {
                CoherenceSyncConfig.Provider.Release(this);
            }

            logger?.Dispose();
        }

        private void OnDisable()
        {
            if (IsClientConnectionInstance())
            {
                return;
            }

            networkInstantiated = EntityState?.NetworkInstantiated ?? false;
            DestroyNetworkEntity();
        }

        private void DestroyNetworkEntity()
        {
            if (CoherenceBridge != null)
            {
                if (CoherenceBridge.IsConnected && !HasStateAuthority && gameObject.scene.isLoaded)
                {
                    logger.Warning(Warning.ToolkitSyncDestroyNonAuthority,
                        $"{name} was destroyed by non-authority while connected, updates will not be correctly applied.",
                        ("entity", EntityState.EntityID));
                }

                CoherenceBridge.OnConnectedInternal -= HandleConnected;

                bridge.EntitiesManager.DestroyAuthorityNetworkEntityState(EntityState);
            }

            HandleChildrenOnNetworkDestruction();

            foreach (var binding in Bindings)
            {
                if (binding != null)
                {
                    binding.Dispose();
                }
            }

            // TODO review if required, now that there's no Reflection Mode
            updater = null;

            ResetBridge();
            ((ICoherenceSync)this).HandleDisconnected();
        }

        private void HandleChildrenOnNetworkDestruction()
        {
            var children = transform.GetComponentsInChildren<CoherenceSync>();
            foreach (var child in children)
            {
                if (child != this)
                {
                    if (preserveChildren)
                    {
                        // unparent and an update is on the way
                        child.SetParent(null);
                    }
                    else if (!HasStateAuthority &&
                             child.updater != null &&
                             !child.updater.TaggedForNetworkedDestruction &&
                             ((ICoherenceSync)child).IsChildFromSyncGroup())
                    {
                        // destroy packet is on the way but we prematurely destroy the Unity object as part of the hierarchy
                        ((ICoherenceSync)child).HandleNetworkedDestruction(true);
                    }
                }
            }
        }

        void ICoherenceSync.HandleNetworkedDestruction(bool destroyedByParent)
        {
            if (updater == null || updater.TaggedForNetworkedDestruction)
            {
                return;
            }

            OnNetworkedDestruction?.Invoke(this);

            HandleChildrenOnNetworkDestruction();

            if (EntityState != null && (EntityState.HasStateAuthority || EntityState.HasInputAuthority))
            {
                // Clear authority before we delete in case it was the RS deciding to
                // delete this entity.
                EntityState.AuthorityType.UpdateValue(AuthorityType.None);
            }

            ResetBridge();

            if (updater != null)
            {
                updater.TaggedForNetworkedDestruction = true;
            }

            if (destroyedByParent)
            {
                return;
            }

            if (gameObject.activeInHierarchy)
            {
                destroyRoutine = StartCoroutine(DestroyThroughInstantiatorDelayed());
            }
            else
            {
                DestroyThroughInstantiator();
            }
        }

        private Coroutine destroyRoutine;

        private IEnumerator DestroyThroughInstantiatorDelayed()
        {
            yield return null;

            DestroyThroughInstantiator();
            destroyRoutine = null;
        }

        internal void DestroyThroughInstantiator()
        {
            CoherenceSyncConfig.Instantiator.Destroy(this);
        }

        /// <summary>
        /// Try to get authority over this entity.
        /// </summary>
        /// <remarks>
        /// Listen to the events <see cref="OnAuthTransferComplete"/> and
        /// <see cref="OnAuthorityRequestRejected"/> to get notified whether the request succeeded or not.
        /// </remarks>
        /// <returns>
        /// <see langword="true"/> if calling this method succeeded. The authority request itself can still fail.
        /// </returns>
        // fixme: Document AuthorityMode
        public bool RequestAuthority(AuthorityType authorityType)
        {
            return bridge.AuthorityManager.RequestAuthority(EntityState, authorityType);
        }

        /// <summary>
        /// Give away authority over this entity to another client.
        /// </summary>
        /// <remarks>
        /// Requires that this client currently has authority.
        /// </remarks>
        /// <param name="clientID">
        /// ClientID of the client that should get authority over this entity.
        /// ClientID can be retrieved from the <see cref="CoherenceBridge.ClientConnections"/>.
        /// </param>
        /// <param name="authorityTransferred">
        /// Type of authority transferred.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if calling this method succeeded. The authority request itself can still fail.
        /// </returns>
        public bool TransferAuthority(ClientID clientID, AuthorityType authorityTransferred = AuthorityType.Full)
        {
            return bridge.AuthorityManager.TransferAuthority(EntityState, clientID, authorityTransferred);
        }

        /// <summary>
        /// Transfers ownership of the game object to the replication server, making it an orphan.
        /// </summary>
        /// <remarks>
        /// The game object must be persistent, transferable and simulated by this client.
        /// The transfer fails if there's no authority over the entity, <see cref="lifetimeType"/> is <see cref="LifetimeType.SessionBased"/>
        /// or <see cref="AuthorityTransfer"/> is <see cref="AuthorityTransferType.NotTransferable"/>.
        /// </remarks>
        /// <returns>
        /// <see langword="true"/> if the authority transfer was successful, or <see langword="false"/> otherwise.
        /// </returns>
        public bool AbandonAuthority()
        {
            return bridge.AuthorityManager.AbandonAuthority(EntityState);
        }

        /// <summary>
        /// Requests authority over an orphaned entity.
        /// </summary>
        /// <remarks>
        /// This method is similar to <see cref="RequestAuthority"/>, but is used for entities that are orphaned.
        /// Adoption requests require <see cref="NetworkEntityState.IsOrphaned"/> to be <see langword="true"/>.
        /// Even if the adoption requests succeeds, the authority request itself can fail.
        /// </remarks>
        /// <returns><see langword="true"/> if the adoption request succeeds; <see langword="false"/> otherwise.</returns>
        /// <example>
        /// <code source="Toolkit/CoherenceSyncSamples.cs" language="csharp" region="Adopt" />
        /// </example>
        public bool Adopt() => bridge.AuthorityManager.Adopt(EntityState);

        /// <summary>
        /// Clear all state of the interpolation used on the
        /// synced variables and properties of this entity.
        /// </summary>
        /// <remarks>
        /// This is useful when the existing interpolation would
        /// interfere with the position and movement of the entity.
        /// For example, after teleporting it to a completely new location.
        /// </remarks>
        /// <param name="setToLastSamples">Set the value of each binding to the latest sampled value.</param>
        public void ResetInterpolation(bool setToLastSamples = false)
        {
            resettingInterpolation = true;

            foreach (var binding in Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (setToLastSamples)
                {
                    binding.SetToLastSample();
                }

                binding.ResetInterpolation();
            }

            resettingInterpolation = false;
        }

        private void OnAuthorityChanged(AuthorityType oldAuthorityType, AuthorityType newAuthorityType)
        {
            if (oldAuthorityType == newAuthorityType)
            {
                logger.Error(Error.ToolkitSyncOnAuthoritySameAuthority,
                    $"{name} tried to set the same authority type",
                    ("authorityType", newAuthorityType));
            }

            bool gainedStateAuthority = !oldAuthorityType.ControlsState() && newAuthorityType.ControlsState();
            bool gainedInputAuthority = !oldAuthorityType.ControlsInput() && newAuthorityType.ControlsInput();
            bool lostStateAuthority = oldAuthorityType.ControlsState() && !newAuthorityType.ControlsState();
            bool lostInputAuthority = oldAuthorityType.ControlsInput() && !newAuthorityType.ControlsInput();

            // When state authority changes, we need to reset the interpolation state
            // and, if authority is gained, we need to make sure that any
            // pending updates are applied immediately.
            if (gainedStateAuthority || lostStateAuthority)
            {
                if (gainedStateAuthority)
                {
                    ResetLastSentData();
                }

                ResetInterpolation(gainedStateAuthority);
            }

            if (lostStateAuthority)
            {
                lastReceivedParent = transform.parent;
            }

            if (gainedStateAuthority || gainedInputAuthority)
            {
                OnBecomesAuthority(gainedStateAuthority, gainedInputAuthority);
            }

            if (lostStateAuthority || lostInputAuthority)
            {
                OnBecomesRemote(lostStateAuthority, lostInputAuthority);
            }

            ConstructGameObjectName();
        }

        private void ResetLastSentData()
        {
            foreach (var binding in Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                binding.ResetLastSentData();
            }
        }

        private void ResetBindings()
        {
            foreach (var binding in Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                binding.MarkAsReadyToSend();
                binding.ResetInterpolation();
            }
        }

        private void OnBecomesAuthority(bool gainedStateAuthority, bool gainedInputAuthority)
        {
            if (gainedStateAuthority)
            {
                RaiseOnStateAuthorityGained();

                TriggerComponentActionsForAuthority();
            }

            if (gainedInputAuthority)
            {
                RaiseOnInputAuthorityGained();
            }
        }

        private void OnBecomesRemote(bool lostStateAuthority, bool lostInputAuthority)
        {
            if (lostStateAuthority)
            {
                RaiseOnStateAuthorityLost();

                TriggerComponentActionsForRemote();
            }

            if (lostInputAuthority)
            {
                RaiseOnInputAuthorityLost();
            }
        }

        private void TriggerComponentActionsForAuthority()
        {
            if (componentActions == null)
            {
                return;
            }

            foreach (var action in componentActions)
            {
                if (action == null)
                {
                    logger.Warning(Warning.ToolkitSyncComponentActionNull,
                        $"Component action is null, please check the actions on '{name}'.");

                    continue;
                }

                if (!action.component)
                {
                    continue;
                }

                try
                {
                    action.OnAuthority();
                }
                catch (Exception exception)
                {
                    logger.Error(Error.ToolkitSyncComponentActionException,
                        $"{action.GetType()}.{nameof(action.OnAuthority)} exception in handler {exception}");
                }
            }
        }

        private void TriggerComponentActionsForRemote()
        {
            if (componentActions == null)
            {
                return;
            }

            foreach (var action in componentActions)
            {
                if (action == null)
                {
                    logger.Warning(Warning.ToolkitSyncComponentActionNull,
                        $"Component action is null, please check the actions on '{name}'.");
                    continue;
                }

                if (!action.component)
                {
                    continue;
                }

                try
                {
                    action.OnRemote();
                }
                catch (Exception exception)
                {
                    logger.Error(Error.ToolkitSyncComponentActionException,
                        $"{action.GetType()}.{nameof(action.OnRemote)} exception in handler: {exception}");
                }
            }
        }

        void ICoherenceSync.ReceiveCommand(IEntityCommand command, MessageTarget target)
        {
            commandsHandler.HandleCommand(command, target);
        }

        private void ActivateBindings()
        {
            foreach (var binding in Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                _ = binding.Activate();
                binding.coherenceSync = this;
            }
        }

        /// <summary>
        /// <see langword="true"/> when this entity is connected or parented to another entity.
        /// </summary>
        // It's possible that the update being processed has a parent change but
        // this isn't applied to the value of ConnectedEntity until all the updates
        // are processed, so things like applying the floating origin need to know that
        // a new parent is about to happen.
        public bool HasParentWithCoherenceSync =>
            ConnectedEntity != null ||
            (((ICoherenceSync)this).Updater.ChangedConnection);

        /// <summary>
        /// Selects between global or local position based on whether
        /// the entity is parented under another coherence entity or not.
        /// </summary>
        /// <exclude/>
        // fixme: Should be internal, blocked by baked sync scripts.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Vector3 coherencePosition
        {
            get
            {
                if (!hasPhysics ||
                    RigidbodyUpdateMode == RigidbodyMode.Direct)
                {
                    return HasParentWithCoherenceSync
                    ? tform.localPosition
                    : tform.position;
                }

                if (syncRigidbody)
                {
                    return syncRigidbody.position;
                }

                return syncRigidbody2D.position;
            }

            set
            {
                if (resettingInterpolation ||
                    !hasPhysics ||
                    RigidbodyUpdateMode == RigidbodyMode.Direct)
                {
                    if (HasParentWithCoherenceSync)
                    {
                        tform.localPosition = value;
                    }
                    else
                    {
                        tform.position = value;
                    }

                    return;
                }

                if (RigidbodyUpdateMode == RigidbodyMode.Interpolated)
                {
                    if (syncRigidbody)
                    {
                        var snapToPosition = PositionBinding.Interpolator.IsBeyondTeleportDistance(syncRigidbody.position, value);
                        if (snapToPosition)
                        {
                            syncRigidbody.position = value;
                        }
                        else
                        {
                            syncRigidbody.MovePosition(value);
                        }
                    }
                    else
                    {
                        var snapToPosition = PositionBinding.Interpolator.IsBeyondTeleportDistance((Vector3)syncRigidbody2D.position, value);
                        if (snapToPosition)
                        {
                            syncRigidbody2D.position = value;
                        }
                        else
                        {
                            syncRigidbody2D.MovePosition(value);
                        }
                    }

                    return;
                }

                if (syncRigidbody)
                {
                    OnRigidbody3DPositionUpdate?.Invoke(value);
                }
                else
                {
                    OnRigidbody2DPositionUpdate?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Selects between global or local rotation based on whether
        /// the entity is parented under another coherence entity or not.
        /// </summary>
        /// <exclude/>
        // fixme: Should be internal, blocked by baked sync scripts.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Quaternion coherenceRotation
        {
            get
            {
                if (!hasPhysics ||
                    RigidbodyUpdateMode == RigidbodyMode.Direct)
                {
                    return HasParentWithCoherenceSync ?
                        tform.localRotation :
                        tform.rotation;
                }

                if (syncRigidbody)
                {
                    return syncRigidbody.rotation;
                }

                return Quaternion.Euler(0f, 0f, syncRigidbody2D.rotation);
            }

            set
            {
                if (resettingInterpolation ||
                    !hasPhysics ||
                    RigidbodyUpdateMode == RigidbodyMode.Direct)
                {
                    if (HasParentWithCoherenceSync)
                    {
                        tform.localRotation = value;
                    }
                    else
                    {
                        tform.rotation = value;
                    }

                    return;
                }

                if (RigidbodyUpdateMode == RigidbodyMode.Interpolated)
                {
                    if (syncRigidbody)
                    {
                        var snapToRotation = RotationBinding.Interpolator.IsBeyondTeleportDistance(syncRigidbody.rotation, value);
                        if (snapToRotation)
                        {
                            tform.rotation = value;
                        }
                        else
                        {
                            syncRigidbody.MoveRotation(value);
                        }
                    }
                    else
                    {
                        var snapToRotation = RotationBinding.Interpolator.IsBeyondTeleportDistance(Quaternion.Euler(0f, 0f, syncRigidbody2D.rotation), value);
                        if (snapToRotation)
                        {
                            syncRigidbody2D.rotation = value.eulerAngles.z;
                        }
                        else
                        {
                            syncRigidbody2D.MoveRotation(value);
                        }
                    }

                    return;
                }

                if (syncRigidbody)
                {
                    OnRigidbody3DRotationUpdate?.Invoke(value);
                }
                else
                {
                    OnRigidbody2DRotationUpdate?.Invoke(value.eulerAngles.z);
                }
            }
        }

        /// <exclude/>
        // fixme: Should be internal, blocked by baked sync scripts.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Vector3 coherenceLocalScale
        {
            get => tform.localScale;
            set => tform.localScale = value;
        }

        void ICoherenceSync.ValidateConnectedEntity()
        {
            Transform parent = tform.parent;
            if (parent == lastReceivedParent)
            {
                return;
            }

            // Check only once. We need to use `lastValidatedParentSet` since
            // `lastValidatedParent` is null initially, which would prevent us
            // from catching the `localParent=null, remoteParent=EntityA` case.
            if (parent == lastValidatedParent && lastValidatedParentSet)
            {
                return;
            }

            lastValidatedParentSet = true;
            lastValidatedParent = parent;

            bool hasParentLocallyButNotOnRemote = parent != null && lastReceivedParent == null;
            if (hasParentLocallyButNotOnRemote)
            {
                bool parentIsNetworked = parent.GetComponentInParent<CoherenceSync>() != null;
                if (!parentIsNetworked)
                {
                    // It is ok to group non-parented entities under non-networked "folder" GOs
                    return;
                }
            }

            logger.Warning(Warning.ToolkitSyncValidateConnectedEntityParent,
                $"Non-authoritative parenting of {name} to {(tform.parent != null ? tform.parent.name : "null")}. " +
                    $"Expected {(lastReceivedParent != null ? lastReceivedParent.name : "null")}. This is not supported and won't be reflected on other clients. " +
                    $"Only the authority of {name} should change the parent.");
        }

        // When the issues around holding back all user-facing callbacks is resolved,
        // this will go away: https://github.com/coherence/unity/issues/3990#issuecomment-1495499188
        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICoherenceSync.RaiseOnConnectedEntityChanged()
        {
            OnNetworkedParentChanged?.Invoke(ConnectedEntity);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICoherenceSync.ApplyNodeBindings()
        {
            if (coherenceNode != null)
            {
                coherenceNode.ApplyBindings();
            }
        }

        private void UpdateConnectedEntityParent()
        {
            var oldParent = ConnectedEntity == null ? null : lastSentParent;

            lastSentParent = tform.parent;

            var parentSync = lastSentParent != null ? lastSentParent.GetComponent<CoherenceSync>() : null;
            var hierarchySync = lastSentParent != null ? lastSentParent.GetComponentInParent<CoherenceSync>() : null;
            var haveCoherenceNode = coherenceNode != null;

            CoherenceSync newConnectedEntity = null;

            if (parentSync != null)
            {
                newConnectedEntity = parentSync;
            }
            else if (hierarchySync != null && haveCoherenceNode)
            {
                newConnectedEntity = hierarchySync;
            }
            else if (hierarchySync != null)
            {
                logger.Warning(Warning.ToolkitSyncUpdateConnectedEntityParentNoSync,
                    $"{name} needs a {nameof(CoherenceNode)} component to be parented under {hierarchySync.name} if not directly parented to {hierarchySync.name}.");
            }

            // Only do this if there's actually parenting to a CS hierarchy.  If the CS is just being parented
            // under whatever to organize (like the GlobalQuery to the Bridge automatically), then we don't warn
            // since it's an un-replicated parent anyway.
            // Also, don't warn when unparenting even though it has the same issue.  The prefab will have to be
            // parented first and using nested prefabs is advanced anyway.
            if ((parentSync != null || hierarchySync != null)
                && (RotationBinding == null || ScaleBinding == null))
            {
                logger.Warning(Warning.ToolkitSyncUpdateConnectedEntityParentMissingBinds,
                    $"{name} needs to replicate the rotation and scale component of the transform or the behaviour of parenting is undefined. " +
                        $"See: {DocumentationLinks.GetDocsUrl(DocumentationKeys.Parenting)}");
            }

            ConnectedEntity = newConnectedEntity;

            // Ensure transform is resent if parent has changed
            updater.OnConnectedEntityChanged();

            // Ensure CoherenceNode bindings will be also sent with parenting changes
            if (coherenceNode != null)
            {
                coherenceNode.MakeBindingsReadyToSend();
            }

            // Shift old samples to the new coordinate system so sampling of movement stays smooth
            TransformSamplesCoordinateSystem(oldParent, tform.parent, true);

            DidSendConnectedEntity?.Invoke(ConnectedEntity);

            // Create new samples with new coordinate system and possible coherenceNode samples too
            updater.SampleBindings();
        }

        void ICoherenceSync.SendConnectedEntity()
        {
            if (RigidbodyUpdateMode != RigidbodyMode.Direct)
            {
                // Only support parenting in Direct mode.
                return;
            }

            if (!ClientInternal.CanSendUpdates(EntityState.EntityID))
            {
                // Don't process this change until the client allows it.
                // Likely in the middle of an authority update.
                return;
            }

            // This is kind of dumb in that if the entity component
            // fields change this needs to be updated too.
            const uint parentChanged = 0b0001;
            const uint posChanged = 0b0010;
            const uint rotChanged = 0b0100;
            const uint scaleChanged = 0b1000;
            uint mask = 0b0;
            var sendAll = false;

            // Any change of parenting situation triggers a resending of
            // connected entity to enable moving an item from one hand
            // to another, etc.
            if (lastSentParent != tform.parent)
            {
                UpdateConnectedEntityParent();

                if (ConnectedEntity == null)
                {
                    ClientInternal.RemoveComponents(EntityState.EntityID, new[] { Impl.GetConnectedEntityComponentIdInternal() });

                    return;
                }

                // if the parent changes and is non null we send all the
                // component fields.
                sendAll = true;
            }

            if (ConnectedEntity == null)
            {
                return;
            }

            var relativePos = tform.parent.position - ConnectedEntity.transform.position;

            if (sendAll ||
                !Mathf.Approximately((relativePos - lastSentRelativePosition).sqrMagnitude, 0.0f))
            {
                mask |= posChanged;

                lastSentRelativePosition = relativePos;
            }

            var relativeRot = tform.parent.rotation * Quaternion.Inverse(ConnectedEntity.transform.rotation);

            if (sendAll ||
                !Mathf.Approximately(Mathf.Abs(Quaternion.Dot(relativeRot, lastSentRelativeRotation)), 1.0f))
            {
                mask |= rotChanged;

                lastSentRelativeRotation = relativeRot;
            }

            // should consider if we want to go all the way with a matrix or use lossy scale.
            var relativeScale = tform.parent.lossyScale - ConnectedEntity.transform.lossyScale;

            if (sendAll ||
                !Mathf.Approximately((relativeScale - lastSentRelativeScale).sqrMagnitude, 0.0f))
            {
                mask |= scaleChanged;

                lastSentRelativeScale = relativeScale;
            }

            if (mask != 0)
            {
                // If there are any changes, always send the parent because of some issue on the
                // RS that is failing if we don't.
                mask |= parentChanged;

                var update = CreateConnectedEntityComponentUpdate(ConnectedEntity, relativePos, relativeRot, relativeScale, mask);
                ClientInternal.UpdateComponents(EntityState.EntityID, update);
            }
        }

        private void ResetConnectedEntity()
        {
            ConnectedEntity = null;
            lastReceivedParent = null;
            lastValidatedParent = null;
            lastValidatedParentSet = false;
            lastSentRelativePosition = Vector2.zero;
            lastSentRelativeRotation = Quaternion.identity;
            lastSentRelativeScale = Vector2.zero;
        }

        private void ResetLastSentParent()
        {
            lastSentParent = null;
        }

        private ICoherenceComponentData[] CreateConnectedEntityComponentUpdate(CoherenceSync coherenceSyncParent, Vector3 pos, Quaternion rot, Vector3 scale, uint mask)
        {
            var parentID = bridge.UnityObjectToEntityId(coherenceSyncParent);
            var comp = Impl.CreateConnectedEntityUpdateInternal(parentID, pos, rot, scale, bridge.NetworkTime.ClientSimulationFrame);
            comp.FieldsMask = mask;

            return new ICoherenceComponentData[] { comp };
        }

        /// <summary>
        ///     For internal use only.
        ///     Any time the parent of the CoherenceSync object is updated
        ///     from the network by any mechanism it should be applied through
        ///     this.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void SetParent(Transform parent)
        {
            tform.SetParent(parent);
            lastReceivedParent = parent;
        }

        private void TransformSamplesCoordinateSystem(Transform oldParent, Transform newParent, bool transformLastSampleToo)
        {
            if (oldParent == newParent)
            {
                return;
            }

            var oldParentMatrix = oldParent != null ? oldParent.localToWorldMatrix : Matrix4x4.identity;
            var newParentMatrix = newParent != null ? newParent.worldToLocalMatrix : Matrix4x4.identity;
            var coordinateShift = newParentMatrix * oldParentMatrix;

            PositionBinding?.TransformSamples(coordinateShift, transformLastSampleToo);
            RotationBinding?.RotateSamples(coordinateShift.rotation, transformLastSampleToo);
        }

        // fixme: Should be internal, blocked by baked sync scripts.
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICoherenceSync.ConnectedEntityChanged(Entity newConnectedEntityID, out bool didChangeParent)
        {
            var oldParent = ConnectedEntity == null ? null : tform.parent;

            if (newConnectedEntityID == Entity.InvalidRelative)
            {
                ConnectedEntity = null;
            }
            else
            {
                var newConnectedEntity = bridge.GetCoherenceSyncForEntity(newConnectedEntityID) as CoherenceSync;
                if (newConnectedEntity == null)
                {
                    logger.Trace($"ConnectedEntityChange: Missing entity {newConnectedEntityID} for {EntityState.EntityID}");

                    didChangeParent = false;

                    return false;
                }

                ConnectedEntity = newConnectedEntity;
            }

            logger.Trace($"ConnectedEntityChange: {EntityState.EntityID} -> {newConnectedEntityID} = {ConnectedEntity}");

            if (coherenceNode != null)
            {
                coherenceNode.UpdateHierarchy();
            }
            else if (oldParent != ConnectedEntity)
            {
                if (ConnectedEntityChangeOverride != null)
                {
                    ConnectedEntityChangeOverride(ConnectedEntity);
                }
                else
                {
                    SetParent(ConnectedEntity.SelfOrNull()?.transform);
                }
            }

            // Transform samples in the sample buffer before the parenting change to the new coordinate system
            // so the movement stays smooth.
            // The last sample isn't transformed because it's in the correct coordinate system.
            // NOTE: The last-sample-is-in-correct-coordinate-system assumption might be wrong
            // if the parent couldn't be resolved in the first frame and more samples arrived before the parent was resolved.
            // But at that point we might already be interpolating over those samples, without having the correct parent,
            // so it's a bit of a lost cause.
            TransformSamplesCoordinateSystem(oldParent, tform.parent, false);

            var newParent = ConnectedEntity == null ? null : tform.parent;
            didChangeParent = oldParent != newParent;

            return true;
        }

        /// <summary>
        /// Get the *first* binding on the component of a specific type.
        /// </summary>
        /// <param name="componentType">The component type (must derive from <see cref="Component"/>).</param>
        /// <param name="bindingName">The name that identifies the binding.</param>
        /// <param name="returnBinding">The binding, if found.</param>
        /// <returns>
        /// <see langword="true"/> if a binding is found. <see langword="false"/> otherwise.
        /// </returns>
        public bool TryGetBinding(Type componentType, string bindingName, out Binding returnBinding)
        {
            foreach (var binding in Bindings)
            {
                if (!binding.UnityComponent)
                {
                    continue;
                }

                if (binding.UnityComponent.GetType() == componentType && binding.Name == bindingName)
                {
                    returnBinding = binding;
                    return true;
                }
            }

            returnBinding = null;
            return false;
        }

        bool ICoherenceSync.TryGetBindingByGuid(string bindingGuid, out Binding outBinding)
        {
            foreach (var binding in Bindings)
            {
                if (!binding.UnityComponent)
                {
                    continue;
                }

                if (binding.guid == bindingGuid)
                {
                    outBinding = binding;
                    return true;
                }
            }

            outBinding = null;
            return false;
        }

        private readonly Dictionary<(Type, string), Binding> bakedBindingsTypeAndNameCache = new Dictionary<(Type, string), Binding>();

        /// <summary>
        /// Returns the binding with matching type and name or null if no match exist.
        /// If no name is supplied, the first binding with matching type is returned.
        /// If multiple matches exist, the first binding in the <see cref="Bindings"/> list is returned.
        /// </summary>
        /// <param name="bindingName">The name of the binding, e.g. "position" (optional)</param>
        /// <typeparam name="T">The type of binding, e.g., <see cref="PositionBinding"/></typeparam>
        /// <returns>A binding matching the given type and name</returns>
        public T GetBakedValueBinding<T>(string bindingName = null) where T : Binding
        {
            if (!UpdateBakedScriptReference())
            {
                logger.Warning(Warning.ToolkitSyncBakedBindingNoScript,
                    ("prefab", name));

                return null;
            }

            if (typeof(T) == typeof(PositionBinding) && PositionBinding != null)
            {
                return BakeBinding(PositionBinding as T);
            }

            if (typeof(T) == typeof(RotationBinding) && RotationBinding != null)
            {
                return BakeBinding(RotationBinding as T);
            }

            if (bakedBindingsTypeAndNameCache.TryGetValue((typeof(T), bindingName), out var cachedBinding))
            {
                return (T)cachedBinding;
            }

            foreach (var binding in Bindings)
            {
                if (binding is not T castedBinding)
                {
                    continue;
                }

                if (bindingName != null && bindingName != castedBinding.Name)
                {
                    continue;
                }

                var bakedBinding = BakeBinding(castedBinding);

                bakedBindingsTypeAndNameCache.Add((typeof(T), bindingName), bakedBinding);
                return bakedBinding;
            }

            bakedBindingsTypeAndNameCache.Add((typeof(T), bindingName), null);
            return null;
        }

        private T BakeBinding<T>(T binding) where T : Binding
        {
            return bakedBindings ? binding : (T)bakedScript.BakeValueBinding(binding);
        }

        internal (int, Binding) IndexOfBindingForDescriptor(Descriptor descriptor, Component component)
        {
            for (int i = 0; i < Bindings.Count; i++)
            {
                var binding = Bindings[i];

                if (binding == null)
                {
                    continue;
                }

                if (binding.Descriptor == descriptor && binding.UnityComponent == component)
                {
                    return (i, binding);
                }
            }

            return (-1, null);
        }

        internal bool HasBindingForDescriptor(Descriptor descriptor, Component component)
        {
            foreach (var binding in Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (binding.Descriptor == descriptor && binding.UnityComponent == component)
                {
                    return true;
                }
            }

            return false;
        }

        internal Binding ShouldUpdateBindingDescriptor(Descriptor descriptor, Component component)
        {
            foreach (var binding in Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (binding.Descriptor == null)
                {
                    continue;
                }

                if (binding.UnityComponent != component)
                {
                    continue;
                }

                if (binding.Descriptor.IsDescriptorRelated(descriptor))
                {
                    return binding;
                }
            }

            return null;
        }

        private Dictionary<(Descriptor, Component), Binding> bindingCache = new();

        internal Binding GetBindingForDescriptor(Descriptor descriptor, Component component)
        {
            // When there's null bindings, or bindings with missing components,
            // this logic will trigger every time (cache size != binding count),
            // possibly slowing down editor performance
            // TODO explicitly set the cache dirty when bindings are added/removed
            if (bindingCache.Count == 0 || bindingCache.Count != Bindings.Count)
            {
                bindingCache.Clear();

                foreach (var binding in Bindings)
                {
                    if (binding == null)
                    {
                        continue;
                    }

                    if (!binding.UnityComponent)
                    {
                        continue;
                    }

                    var key = (binding.Descriptor, binding.UnityComponent);
                    // If adding to the dict fails, don't log here, since it will spam the console (lots of accesses).
                    // Instead, CoherenceSyncUtils.IsBindingValid should report that there's invalid bindings.
                    _ = bindingCache.TryAdd(key, binding);
                }
            }

            return bindingCache.GetValueOrDefault((descriptor, component));
        }

        private void ClearEntityState()
        {
            if (EntityState == null)
            {
                return;
            }

            uuidToBeDeRegistered = EntityState.CoherenceUUID;
            EntityState.AuthorityType.OnValueUpdated -= OnAuthorityChanged;
            EntityState = null;
        }

        internal bool ValidateArchetype()
        {
            bool changed = false;

            if (archetype == null)
            {
                archetype = new ToolkitArchetype();
                archetype.Setup(this);

                changed = true;
            }

            changed |= archetype.UpdateBoundVariables(this);

            return changed;
        }

        void ICoherenceSync.RaiseOnAuthorityRequested(AuthorityRequest request)
        {
            if (OnAuthorityRequest is { Count: > 0 })
            {
                if (OnAuthorityRequest.Count > 1)
                {
                    logger.Warning(Warning.ToolkitSyncAuthorityRequestMultipleListeners);
                }
                try
                {
                    OnAuthorityRequest.Invoke(request, this);
                }
                catch (Exception e)
                {
                    HandleException(nameof(OnAuthorityRequest), e);
                    request.Reject();
                }

                return;
            }

            if (OnAuthorityRequested != null)
            {
                try
                {
                    var accept = OnAuthorityRequested.Invoke(request.RequesterID, request.AuthorityType, this);
                    request.Respond(new AuthorityRequestResponse(accept));
                }
                catch (Exception e)
                {
                    HandleException(nameof(OnAuthorityRequested), e);
                    request.Reject();
                }

                return;
            }

            request.Respond(new AuthorityRequestResponse(approveAuthorityTransferRequests));
        }

        internal void RaiseOnAuthorityRequestRejected(AuthorityType authorityType)
        {
            try
            {
                OnAuthorityRequestRejected?.Invoke(authorityType);
            }
            catch (Exception e)
            {
                HandleException(nameof(OnAuthorityRequestRejected), e);
            }
        }

        internal void RaiseOnAuthorityTranferred()
        {
            try
            {
                OnAuthTransferComplete?.Invoke();
            }
            catch (Exception e)
            {
                HandleException(nameof(OnAuthTransferComplete), e);
            }
        }

        private void RaiseOnStateAuthorityGained()
        {
            try
            {
                OnStateAuthority?.Invoke();
            }
            catch (Exception e)
            {
                HandleException(nameof(OnStateAuthority), e);
            }
        }

        private void RaiseOnStateAuthorityLost()
        {
            try
            {
                OnStateRemote?.Invoke();
            }
            catch (Exception e)
            {
                HandleException(nameof(OnStateRemote), e);
            }
        }

        private void RaiseOnInputAuthorityGained()
        {
            try
            {
                OnInputAuthority?.Invoke();
            }
            catch (Exception e)
            {
                HandleException(nameof(OnInputAuthority), e);
            }
        }

        private void RaiseOnInputAuthorityLost()
        {
            Input.SelfOrNull()?.Buffer.Reset();

            try
            {
                OnInputRemote?.Invoke();
            }
            catch (Exception e)
            {
                HandleException(nameof(OnInputRemote), e);
            }
        }

        bool ICoherenceSync.ShiftOrigin(Vector3d delta)
        {
            PositionBinding.ShiftSamples(delta.ToUnityVector3());

            Vector3d currentPosition;
            if (syncRigidbody)
            {
                currentPosition = syncRigidbody.position.ToVector3d();
            }
            else if (syncRigidbody2D)
            {
                currentPosition = ((Vector3)syncRigidbody2D.position).ToVector3d();
            }
            else
            {
                currentPosition = transform.position.ToVector3d();
            }

            Vector3d newPosition = currentPosition - delta;

            logger.Debug($"Shifting {name} from {currentPosition} to {newPosition.ToUnityVector3()}");

            transform.position = newPosition.ToUnityVector3();
            if (syncRigidbody)
            {
                // Looking at the Unity docs for Rigidbody.MovePosition and Rigidbody.position, it says that
                // MovePosition will apply interpolation settings to the transform, while just setting the position
                // with .position will teleport the object. This is behaviour is correct only in the FixedUpdate.
                // If .position is used in normal Update, it will still apply interpolation so we have to disable it
                // to force it to actually teleport the object and then reenable it again after setting the position.
                var oldInterpolation = syncRigidbody.interpolation;
                syncRigidbody.interpolation = RigidbodyInterpolation.None;

                syncRigidbody.position = newPosition.ToUnityVector3();

                syncRigidbody.interpolation = oldInterpolation;
            }
            else if (syncRigidbody2D)
            {
                // Same as the Rigidbody3D.
                var oldInterpolation = syncRigidbody2D.interpolation;
                syncRigidbody2D.interpolation = RigidbodyInterpolation2D.None;

                syncRigidbody2D.position = newPosition.ToUnityVector3();

                syncRigidbody2D.interpolation = oldInterpolation;
            }

            if (HasStateAuthority)
            {
                _ = ((ICoherenceSync)this).Updater.TryFlushPosition(false); // Flushes new relative position to the change buffer
            }

            return true;
        }

        bool ICoherenceSync.ShouldShift()
        {
            // Do not shift entities with synced parents. When the root entity shifts, its children will follow.
            if (HasParentWithCoherenceSync)
            {
                logger.Debug($"Not shifting {name} because it has a networked parent");
                return false;
            }

            //  Entities configured to move together with the Floating Origin do not shift on the local client.
            if (tform.parent == null && floatingOriginMode == FloatingOriginMode.MoveWithFloatingOrigin && !IsPositionNetworkedControlled())
            {
                logger.Debug($"Not shifting {name} because it has state authority and set to stay Relative to Floating Origin.");
                return false;
            }

            // Do not shift if parented (unless parented mode is set to Absolute, e.g., GlobalQuery will shift although it has a non-synced parent)
            if (tform.parent != null && floatingOriginParentedMode == FloatingOriginMode.MoveWithFloatingOrigin)
            {
                logger.Debug($"Not shifting parented {name} that goes with the flow");
                return false;
            }

            return true;
        }

        private bool IsPositionNetworkedControlled()
        {
            if (HasStateAuthority)
            {
                return false;
            }

            return !PositionBinding.IsCurrentlyPredicted();
        }

        private void HandleException(string function, Exception exception)
        {
            logger.Error(Error.ToolkitSyncException,
                ("function", function),
                ("exception", exception));
        }

#if UNITY_EDITOR
        /// <summary>
        /// Contains names of serialized properties found in CoherenceSync.
        /// Can be used in the editor with SerializedObject.FindProperty etc.
        /// </summary>
        internal static class Property
        {
            public const string updater = nameof(CoherenceSync.updater);

            public const string coherenceSyncConfig = nameof(CoherenceSync.coherenceSyncConfig);
            public const string bakedScriptType = nameof(CoherenceSync.bakedScriptType);
#pragma warning disable CS0618
            public const string bindings = nameof(CoherenceSync.bindings);
#pragma warning restore CS0618
            public const string archetype = nameof(CoherenceSync.archetype);
            public const string componentActions = nameof(CoherenceSync.componentActions);
        }
#endif
    }
}
