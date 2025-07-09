// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Entities;

    /// <summary>
    /// Holds the state of a network entity.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="CoherenceSync"/>, <see cref="CoherenceBridge"/>, <see cref="EntitiesManager"/>,
    /// <see cref="UniquenessManager"/>, <see cref="AuthorityManager"/> and <see cref="FloatingOriginManager"/>.
    /// </remarks>
    public class NetworkEntityState
    {
        /// <summary>
        /// The entity information on the current client.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This entity is not synced across clients.
        /// </para>
        /// <para>
        /// If you need to sync or send an entity reference to another client,
        /// bind to a CoherenceSync instead (can be `GameObject` or a `UnityEngine.Transform` reference, as long
        /// as the hierarchy has a CoherenceSync).
        /// </para>
        /// </remarks>
        public Entity EntityID { get; }

        /// <summary>
        /// An identifier used to ensure that CoherenceSync objects with <see cref="CoherenceSync.uniquenessType"/>
        /// set to <see cref="CoherenceSync.UniquenessType.NoDuplicates"/> only have a single instance
        /// across all clients.
        /// </summary>
        public string CoherenceUUID { get; internal set; }

        /// <summary>
        /// Authority type for this entity, on this client.
        /// </summary>
        public ObservableAuthorityType AuthorityType { get; }

        /// <summary>
        /// <see langword="true"/> if no client has authority over the entity.
        /// </summary>
        public bool IsOrphaned { get; internal set; }

        /// <summary>
        /// <see langword="true"/> if the entity originates from network replication (created by another client).
        /// <see langword="false"/> if the entity was created locally.
        /// </summary>
        public bool NetworkInstantiated { get; }

        /// <summary>
        /// <see langword="true"/> if the entity is created by the <see cref="ClientConnection"/> for this client.
        /// </summary>
        public bool IsMyClientConnection => ClientConnection?.IsMyConnection ?? false;

        /// <summary>
        /// Associated client connection, if this entity represents one.
        /// </summary>
        public CoherenceClientConnection ClientConnection;

        /// <summary>
        /// Time at which last <see cref="CoherenceSync.Adopt"/> operation was performed.
        /// </summary>
        /// <remarks>
        /// It stores the result of <see cref="UnityEngine.Time.unscaledTime"/>.
        /// </remarks>
        public float LastTimeRequestedOrphanAdoption { get; internal set; }

        /// <summary>
        /// The associated <see cref="CoherenceSync"/> reference.
        /// </summary>
        public ICoherenceSync Sync { get; internal set; }

        /// <summary>
        /// <see langword="true"/> if the client has state authority over the entity. State authority is an actual owner of the entity and controls all the synced variables.
        /// </summary>
        /// <remarks>
        /// The state authority is the actual owner of the entity, and controls the state of all the synced variables.
        /// That's to say, state for the entity is read from the client that owns its state authority.
        /// </remarks>
        /// <seealso cref="HasInputAuthority"/>
        public bool HasStateAuthority => AuthorityType.Value.ControlsState();

        /// <summary>
        /// <see langword="true"/> if the client has input authority over the entity.
        /// </summary>
        /// <remarks>
        /// Input authority can produce inputs via <see cref="CoherenceInput"/> which are then sent to the state authority who processes them.
        /// </remarks>
        /// <seealso cref="HasStateAuthority"/>
        public bool HasInputAuthority => AuthorityType.Value.ControlsInput();

        internal NetworkEntityState(Entity entityId, AuthorityType authority, bool isOrphaned, bool networkInstantiated,
            ICoherenceSync sync, string uuid)
        {
            EntityID = entityId;
            AuthorityType = new ObservableAuthorityType(authority);
            Sync = sync;
            IsOrphaned = isOrphaned;
            NetworkInstantiated = networkInstantiated;
            CoherenceUUID = uuid ?? string.Empty;
        }
    }

    /// <summary>
    /// Holds an <see cref="AuthorityType"/> value, and notifies when it changes.
    /// Used by <see cref="NetworkEntityState"/>.
    /// </summary>
    public class ObservableAuthorityType : Observable<AuthorityType>
    {
        /// <summary>
        /// Creates an observable authority type.
        /// </summary>
        /// <param name="authority">The value to hold.</param>
        public ObservableAuthorityType(AuthorityType authority) : base(authority)
        {
        }
    }
}
