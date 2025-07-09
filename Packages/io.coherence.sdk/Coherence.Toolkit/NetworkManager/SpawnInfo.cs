// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Bindings;
    using Connection;
    using Entities;
    using System;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// The spawn information for a network entity. Used when coherence needs to instantiate a CoherenceSync prefab and
    /// link it to a new network entity.
    /// </summary>
    /// <seealso cref="INetworkObjectInstantiator"/>
    public struct SpawnInfo
    {
        /// <summary>
        /// The assetID is the GUID reference given to each file in your Unity project's Asset folder.
        /// </summary>
        /// <remarks>
        /// These IDs are located in the
        /// <see href="https://docs.unity3d.com/Manual/AssetMetadata.html">.meta data file</see>
        /// for each asset. It is also reference in the CoherenceSyncConfig file's
        /// <see cref="CoherenceSyncConfig.ID">ID</see> field.</remarks>
        public string assetId;

        /// <summary>
        /// If the entity is part of a <see cref="PrefabSyncGroup"/> this will be set to <see langword="true" />.
        /// </summary>
        /// <remarks>
        /// See <see href="https://docs.coherence.io/manual/parenting-network-entities/nesting-prefabs-at-edit-time">Nesting Prefabs at Edit time</see> for more information.
        /// </remarks>
        public bool isFromGroup;

        /// <summary>
        /// The world space position of the entity.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The rotation of the entity in world space.
        /// </summary>
        public Quaternion? rotation;

        /// <summary>
        /// The network <see cref="Entity"/> that will be spawned using the information in this class.
        /// </summary>
        /// <remarks>
        /// An entity is an object that is networked, and whose properties are set to sync over the network. The
        /// Replication Server, being engine agnostic – doesn't think in objects/Prefabs/Actors, but in entities. See
        /// also <see href="https://docs.coherence.io/manual/advanced-topics/schema-explained/specification#entity">entity</see>.
        /// </remarks>
        public Entity connectedEntity;

        /// <summary>
        /// The ID that identifies the client, assigned by the replication server.
        /// </summary>
        public ClientID? clientId;

        /// <summary>
        /// The unique identifier of this entity. See <see cref="CoherenceSync.ManualUniqueId"/>
        /// </summary>
        public string uniqueId;

        /// <summary>
        /// How the entity will connect. See <see cref="CoherenceBridge.EnableClientConnections"/>
        /// </summary>
        /// <remarks>
        /// Only set for client connections, i.e. when <see cref="CoherenceBridge.EnableClientConnections"/> is <see langword="true" />.
        /// </remarks>
        public ConnectionType? connectionType;

        /// <summary>
        /// The prefab of the entity.
        /// </summary>
        public ICoherenceSync prefab;

        /// <summary>
        /// The bridge associated with the network entity.
        /// </summary>
        public ICoherenceBridge bridge;

        internal ComponentUpdates ComponentUpdates;

        /// <summary>
        /// Returns the initial data that will be applied to a network instantiated entity's bindings.
        /// This method is intended to query pre-synced data during <see cref="INetworkObjectInstantiator.Instantiate"/>.
        /// It can be useful when it is necessary to know a field's value ahead of time, e.g., to identify the proper pool or asset to use when instantiating a remote entity.
        /// If multiple matches exist for the given binding name and type, the first binding in the <see cref="CoherenceSync.Bindings"/> list is returned.
        /// </summary>
        /// <param name="bindingName">The name of the binding, e.g. "position"</param>
        /// <typeparam name="T">The binding's value type, e.g. "Vector3"</typeparam>
        /// <returns>Returns the initial synced value for a given binding.</returns>
        /// <exception cref="Exception">If the binding does not exist, or isn't synced with the initial packet, an exception is thrown.</exception>
        public T GetBindingValue<T>(string bindingName)
        {
            var binding = prefab.GetBakedValueBinding<ValueBinding<T>>(bindingName);
            if (binding == null)
            {
                throw new Exception($"Could not find binding: {bindingName}");
            }

            var (_, componentData) = ComponentUpdates.Store.FirstOrDefault(kvp => kvp.Value.Data.GetType() == binding.CoherenceComponentType);
            if (componentData.Data == default)
            {
                throw new Exception($"Could not find value for binding: {bindingName}");
            }

            return binding.PeekComponentData(componentData.Data);
        }
    }
}
