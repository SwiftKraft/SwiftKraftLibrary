// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using Connection;

#if COHERENCE_ENABLE_MULTI_ROOM_SIMULATOR
    [UnityEngine.AddComponentMenu("coherence/Events/Coherence Connection Event Handler")]
#endif
    [NonBindable]
    [Obsolete("ConnectionEventHandler is being deprecated, and will be completely removed in future releases.")]
    [Deprecated("17/03/2024", 1, 6, 0)]
    public sealed class ConnectionEventHandler : CoherenceBehaviour
    {
        // for components, we don't expose direct creation of instances - add as component instead
        private ConnectionEventHandler()
        {
        }

        [Tooltip("Additionally registers the CoherenceBridge associated by the CoherenceScene (in the target scene)")]
        public CoherenceSceneLoader loader;

        private CoherenceBridge bridge;

        [Header("Client")]
        public GameObject[] deactivateOnClientConnected;
        public GameObject[] destroyOnClientConnected;
        public UnityEvent<CoherenceBridge> onClientConnected = new UnityEvent<CoherenceBridge>();
        public UnityEvent<CoherenceBridge, ConnectionCloseReason> onClientDisconnected = new UnityEvent<CoherenceBridge, ConnectionCloseReason>();

        [Header("Simulator")]
        public GameObject[] deactivateOnSimulatorConnected;
        public GameObject[] destroyOnSimulatorConnected;
        public UnityEvent<CoherenceBridge> onSimulatorConnected = new UnityEvent<CoherenceBridge>();
        public UnityEvent<CoherenceBridge, ConnectionCloseReason> onSimulatorDisconnected = new UnityEvent<CoherenceBridge, ConnectionCloseReason>();

        [Header("Global")]
        public GameObject[] deactivateOnConnected;
        public GameObject[] destroyOnConnected;
        public UnityEvent<CoherenceBridge> onConnected = new UnityEvent<CoherenceBridge>();
        public UnityEvent<CoherenceBridge, ConnectionCloseReason> onDisconnected = new UnityEvent<CoherenceBridge, ConnectionCloseReason>();

        private void Awake()
        {
            _ = CoherenceBridgeStore.TryGetBridge(gameObject.scene, out bridge);
            if (loader)
            {
                loader.onLoaded.AddListener(Register);
                loader.onBeforeUnload.AddListener(Unregister);
            }
        }

        private void OnEnable()
        {
            Register(bridge);
        }

        private void OnDisable()
        {
            Unregister(bridge);
        }

        private void Register(CoherenceBridge bridge)
        {
            if (!bridge)
            {
                return;
            }

            bridge.onConnected.AddListener(OnConnected);
            bridge.onDisconnected.AddListener(OnDisconnected);
        }

        private void Unregister(CoherenceBridge bridge)
        {
            if (!bridge)
            {
                return;
            }

            bridge.onConnected.RemoveListener(OnConnected);
            bridge.onDisconnected.RemoveListener(OnDisconnected);
        }

        private void OnConnected(CoherenceBridge bridge)
        {
            onConnected?.Invoke(bridge);
            foreach (var go in deactivateOnConnected)
            {
                if (!go)
                {
                    continue;
                }

                go.SetActive(false);
            }
            foreach (var go in destroyOnConnected)
            {
                if (!go)
                {
                    continue;
                }

                Destroy(go);
            }

            switch (bridge.Client.ConnectionType)
            {
                case ConnectionType.Client:
                    OnClientConnected(bridge);
                    break;
                case ConnectionType.Simulator:
                    OnSimulatorConnected(bridge);
                    break;
                case ConnectionType.Replicator:
                    break;
                default:
                    break;
            }
        }

        private void OnDisconnected(CoherenceBridge bridge, ConnectionCloseReason closeReason)
        {
            onDisconnected?.Invoke(bridge, closeReason);
            switch (bridge.Client.ConnectionType)
            {
                case ConnectionType.Client:
                    OnClientDisconnected(bridge, closeReason);
                    break;
                case ConnectionType.Simulator:
                    OnSimulatorDisconnected(bridge, closeReason);
                    break;
                case ConnectionType.Replicator:
                    break;
                default:
                    break;
            }
        }

        private void OnClientConnected(CoherenceBridge bridge)
        {
            onClientConnected?.Invoke(bridge);
            foreach (var go in deactivateOnClientConnected)
            {
                if (!go)
                {
                    continue;
                }

                go.SetActive(false);
            }
            foreach (var go in destroyOnClientConnected)
            {
                if (!go)
                {
                    continue;
                }

                Destroy(go);
            }
        }

        private void OnClientDisconnected(CoherenceBridge bridge, ConnectionCloseReason closeReason)
        {
            onClientDisconnected?.Invoke(bridge, closeReason);
        }

        private void OnSimulatorConnected(CoherenceBridge bridge)
        {
            onSimulatorConnected?.Invoke(bridge);
            foreach (var go in deactivateOnSimulatorConnected)
            {
                if (!go)
                {
                    continue;
                }

                go.SetActive(false);
            }
            foreach (var go in destroyOnSimulatorConnected)
            {
                if (!go)
                {
                    continue;
                }

                Destroy(go);
            }
        }

        private void OnSimulatorDisconnected(CoherenceBridge bridge, ConnectionCloseReason closeReason)
        {
            onSimulatorDisconnected?.Invoke(bridge, closeReason);
        }
    }
}
