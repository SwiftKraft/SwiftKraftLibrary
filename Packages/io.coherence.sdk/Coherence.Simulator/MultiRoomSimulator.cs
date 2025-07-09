// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Simulator
{
    using System;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;
    using Log;
    using Logger = Log.Logger;
    using Toolkit;
    using Connection;
    using System.Collections.Generic;

#if COHERENCE_ENABLE_MULTIROOM_SIMULATOR
    [UnityEngine.AddComponentMenu("coherence/Multi-Room Simulators/Multi-Room Simulator")]
#endif
    [NonBindable]
    [Obsolete("Multi-Room Simulators are being deprecated, and will be completely removed in future releases.")]
    [Deprecated("17/03/2024", 1, 6, 0)]
    public sealed class MultiRoomSimulator : CoherenceBehaviour
    {
        public static MultiRoomSimulator Instance { get; private set; }

        private readonly Dictionary<EndpointData, CoherenceSceneLoader> loaders = new();
        private readonly Logger logger = Log.GetLogger<MultiRoomSimulator>();

        public string sceneName;
        public LocalPhysicsMode localPhysicsMode;
        public UnloadSceneOptions unloadSceneOptions;
        public UnityEvent<CoherenceBridge> onLoaded = new();
        public UnityEvent<CoherenceBridge> onBeforeUnload = new();

        private HttpServer server;

        private bool process;
        private List<EndpointData> endpointDatas = new();

        protected override void Reset()
        {
            base.Reset();

            sceneName = gameObject.scene.name;
            localPhysicsMode = LocalPhysicsMode.Physics3D;
        }

        private void OnEnable()
        {
            if (Instance)
            {
                enabled = false;
                return;
            }

            logger.Info("Starting",
                ("sceneName", sceneName),
                ("localPhysicsMode", localPhysicsMode),
                ("unloadSceneOptions", unloadSceneOptions));

            Instance = this;

            server = new HttpServer();
            server.OnJoinRequested += OnJoinRequested;
            var port = SimulatorUtility.HttpServerPort;
            if (port == -1)
            {
                port = RuntimeSettings.Instance.LocalHttpServerPort;
            }
            StartServer(port);
        }

        private async void StartServer(int port)
        {
            logger.Info("HTTP Server starting", ("port", port));

            var result = await server.Start(port);
            if (!result)
            {
                logger.Error(Error.SimulatorMRSStartFailure,
                    "Failed to start HTTP Server on " + port);
            }
        }

        private void OnDisable()
        {
            if (server != null)
            {
                server.OnJoinRequested -= OnJoinRequested;
                server.Stop();
                server = null;

                logger.Info("HTTP Server stopped");
            }

            foreach (var pair in loaders)
            {
                var loader = pair.Value;
                if (loader)
                {
                    _ = loader.Unload();
                }
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnJoinRequested(EndpointData endpointData)
        {
            process = true;
            endpointDatas.Add(endpointData);
        }

        private void Update()
        {
            if (process)
            {
                foreach (var endpointData in endpointDatas)
                {
                    _ = CreateLoader(endpointData);
                }
                endpointDatas.Clear();
                process = false;
            }
        }

        private CoherenceSceneLoader CreateLoader(EndpointData endpointData)
        {
            logger.Info("Received room join request", ("roomEndpoint", endpointData));

            if (loaders.TryGetValue(endpointData, out CoherenceSceneLoader loader))
            {
                logger.Warning(Warning.SimulatorMRSCreateConnected, ("roomId", endpointData.roomId));
            }
            else
            {
                logger.Info("Loading scene", ("roomId", endpointData.roomId), ("uniqueRoomId", endpointData.uniqueRoomId));

                loader = CoherenceSceneLoader.CreateInstance().Configure(sceneName, ConnectionType.Simulator);
                loader.localPhysicsMode = localPhysicsMode;
                loader.unloadSceneOptions = unloadSceneOptions;
                loader.onLoaded.AddListener(bridge => OnLoaded(bridge, endpointData));
                loader.onBeforeUnload.AddListener(bridge => OnBeforeUnload(bridge, endpointData));
                _ = loader.Load(endpointData);
                loaders.Add(endpointData, loader);
            }

            return loader;
        }

        private void OnLoaded(CoherenceBridge bridge, EndpointData endpointData)
        {
            logger.Info("Scene loaded", ("roomId", endpointData.roomId), ("uniqueRoomId", endpointData.uniqueRoomId));
            onLoaded?.Invoke(bridge);
        }

        private void OnBeforeUnload(CoherenceBridge bridge, EndpointData endpointData)
        {
            logger.Info("Unloading scene", ("roomId", endpointData.roomId), ("uniqueRoomId", endpointData.uniqueRoomId));
            onBeforeUnload?.Invoke(bridge);
        }
    }
}
