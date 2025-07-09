// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections;
    using Cloud;
    using Common;
    using Connection;
    using Entities;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    using Logger = Log.Logger;

    public interface ICoherenceBridge
    {
        event Action OnFixedNetworkUpdate;
        event Action OnLateFixedNetworkUpdate;
        event Action OnTimeReset;

        long ClientFixedSimulationFrame { get; }
        double NetworkTimeAsDouble { get; }
        Scene? InstantiationScene { get; }
        bool IsSimulatorOrHost { get; }
        bool IsConnected { get; }
        bool IsConnecting { get; }

        bool EnableClientConnections { get; set; }
        bool CreateGlobalQuery { get; set; }
        bool HasActiveGlobalQuery { get; }
        Transform Transform { get; }
        ClientID ClientID { get; }
        CoherenceClientConnectionManager ClientConnections { get; }
        IClient Client { get; }
        Scene Scene { get; }

        private const string StartCoroutineObsoleteMessage = "Coroutines should no longer be started via the " + nameof(ICoherenceBridge) + " interface. Use a " + nameof(MonoBehaviour) + " instead.";
        [Obsolete(StartCoroutineObsoleteMessage, false), Deprecated("07/2024", 1, 2, 4, Reason = StartCoroutineObsoleteMessage)]
        Coroutine StartCoroutine(IEnumerator routine);

        CoherenceSyncConfig GetClientConnectionEntry();
        CoherenceSyncConfig GetSimulatorConnectionEntry();
        CoherenceInputManager InputManager { get; }
        INetworkTime NetworkTime { get; }
        event Action<FloatingOriginShiftArgs> OnAfterFloatingOriginShifted;
        ConnectionType ConnectionType { get; }
        EntitiesManager EntitiesManager { get; }
        UniquenessManager UniquenessManager { get; }
        AuthorityManager AuthorityManager { get; }
        CloudService CloudService { get; }
        bool AutoLoginAsGuest { get; }
        string NetworkPrefix { get; }
        Logger Logger { get; }

        event Action<ICoherenceBridge> OnConnectedInternal;

        ICoherenceSync GetCoherenceSyncForEntity(Entity id);
        void OnNetworkEntityDestroyedInvoke(NetworkEntityState state, DestroyReason destroyReason);
        void OnNetworkEntityCreatedInvoke(NetworkEntityState state);

        Entity UnityObjectToEntityId(GameObject from);
        Entity UnityObjectToEntityId(Transform from);
        Entity UnityObjectToEntityId(ICoherenceSync from);
        GameObject EntityIdToGameObject(Entity from);
        Transform EntityIdToTransform(Entity from);
        RectTransform EntityIdToRectTransform(Entity from);
        CoherenceSync EntityIdToCoherenceSync(Entity from);

        void Disconnect();
        bool TranslateFloatingOrigin(Vector3d translation);
        bool TranslateFloatingOrigin(Vector3 translation);
        bool SetFloatingOrigin(Vector3d newOrigin);
        Vector3d GetFloatingOrigin();

#if !ENABLE_INPUT_SYSTEM
        public FixedUpdateInput FixedUpdateInput { get; }
#endif
    }
}
