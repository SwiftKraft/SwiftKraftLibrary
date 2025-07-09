// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Connection;
    using Log;
    using Logger = Log.Logger;
    using UnityEngine.SceneManagement;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Handles loading one scene additively, and holds instructions for how that loaded scene should connect to the network.
    /// </summary>
    [AddComponentMenu("coherence/Scene Loading/Coherence Scene Loader")]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.7/manual/multiple-connections-within-a-game-instance")]
    public sealed class CoherenceSceneLoader : CoherenceBehaviour
    {
        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceSceneLoader()
        {
        }

        private readonly Logger logger = Log.GetLogger<CoherenceSceneLoader>();

        private static EndpointData lastEndpointData;
        private static int loadOperations;

        internal static bool loading;
        internal static Dictionary<Scene, CoherenceSceneData> dataMap = new Dictionary<Scene, CoherenceSceneData>();
        internal static Dictionary<Scene, CoherenceSceneLoader> loaderMap = new Dictionary<Scene, CoherenceSceneLoader>();

        public static List<Scene> scenes = new List<Scene>();
        private Scene scene;

        [Tooltip("If enabled, the loader will load/unload on CohereceBridge connections/disconnections. Otherwise, the loader only responds to the Load/Unload API.")]
        [SerializeField] private bool attach;

        public bool Attach
        {
            get => attach;
            set
            {
                attach = value;
                UpdateAttachState();
            }
        }

        [Header("Scene Loading Settings")]
        public ConnectionType connectionType = ConnectionType.Simulator;
        public string sceneName;
        public LocalPhysicsMode localPhysicsMode;
        public UnloadSceneOptions unloadSceneOptions;

        // invoked by the listener
        public UnityEvent<CoherenceBridge> onLoaded = new UnityEvent<CoherenceBridge>();
        public UnityEvent<CoherenceBridge> onBeforeUnload = new UnityEvent<CoherenceBridge>();

        public Coroutine LoadingCoroutine { get; private set; }
        public Coroutine UnloadingCoroutine { get; private set; }

        public Scene Scene => scene;

        private CoherenceBridge bridge;

        private bool IsLoaded => dataMap.ContainsKey(gameObject.scene);

        private bool isEnabled;

        public static CoherenceSceneLoader CreateInstance()
        {
            return CreateInstance("Local Client Loader");
        }

        public static CoherenceSceneLoader CreateInstance(string name)
        {
            return CreateInstance(new GameObject(name));
        }

        public static CoherenceSceneLoader CreateInstance(GameObject go)
        {
            if (!go)
            {
                return null;
            }

            var r = go.AddComponent<CoherenceSceneLoader>();
            return r;
        }

        public CoherenceSceneLoader Configure(CoherenceSceneLoaderConfig config)
        {
            sceneName = config.sceneName;
            connectionType = config.connectionType;
            localPhysicsMode = config.localPhysicsMode;
            unloadSceneOptions = config.unloadSceneOptions;
            return this;
        }

        public CoherenceSceneLoader Configure(string sceneName)
        {
            var config = new CoherenceSceneLoaderConfig
            {
                sceneName = sceneName
            };
            return Configure(config);
        }

        public CoherenceSceneLoader Configure(string sceneName, ConnectionType connectionType)
        {
            var config = new CoherenceSceneLoaderConfig
            {
                sceneName = sceneName,
                connectionType = connectionType
            };
            return Configure(config);
        }

        public CoherenceSceneLoader Load(EndpointData endpointData)
        {
            var data = new CoherenceSceneData
            {
                SceneName = sceneName,
                ConnectionType = connectionType,
                EndpointData = endpointData,
                LocalPhysicsMode = localPhysicsMode,
            };

            LoadingCoroutine = StartCoroutine(DoLoadScene(data));
            return this;
        }

        public CoherenceSceneLoader Unload()
        {
            UnloadingCoroutine = StartCoroutine(DoUnloadScene());
            return this;
        }

        private void OnValidate()
        {
            // OnValidate is called whenever the script’s properties are set, including when an object is deserialized,
            // which can occur at various times, such as when you open a scene in the Editor and after a domain reload.
            if (Application.isPlaying && isEnabled)
            {
                UpdateAttachState();
            }
        }

        protected override void Reset()
        {
            base.Reset();

            connectionType = ConnectionType.Simulator;
            sceneName = gameObject.scene.name;
#if UNITY_EDITOR
            switch (EditorSettings.defaultBehaviorMode)
            {
                case EditorBehaviorMode.Mode3D:
                    localPhysicsMode = LocalPhysicsMode.Physics3D;
                    break;
                case EditorBehaviorMode.Mode2D:
                    localPhysicsMode = LocalPhysicsMode.Physics2D;
                    break;
                default:
                    localPhysicsMode = ~LocalPhysicsMode.None;
                    break;
            }
#else
            localPhysicsMode = ~LocalPhysicsMode.None;
#endif
            unloadSceneOptions = UnloadSceneOptions.None;
        }

        private void UpdateAttachState()
        {
            if (bridge != null)
            {
                bridge.Client.OnConnected -= OnConnect;
                bridge.Client.OnDisconnected -= OnDisconnect;
                bridge.Client.OnConnectionError -= OnConnectionError;
                bridge.Client.OnConnectedEndpoint -= OnConnectedEndpoint;
            }

            if (attach && bridge == null)
            {
                if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out bridge))
                {
                    logger.Warning(Warning.ToolkitSceneLoaderMissingBridge,
                        ("scene", gameObject.scene.name));
                }
            }


            if (bridge?.Client != null)
            {
                bridge.Client.OnConnected += OnConnect;
                bridge.Client.OnDisconnected += OnDisconnect;
                bridge.Client.OnConnectionError += OnConnectionError;
                bridge.Client.OnConnectedEndpoint += OnConnectedEndpoint;
            }
        }

        private void OnEnable()
        {
            isEnabled = true;

            UpdateAttachState();
        }

        private void OnDisable()
        {
            isEnabled = false;

            if (bridge == null)
            {
                return;
            }

            bridge.Client.OnConnected -= OnConnect;
            bridge.Client.OnDisconnected -= OnDisconnect;
            bridge.Client.OnConnectionError -= OnConnectionError;
            bridge.Client.OnConnectedEndpoint -= OnConnectedEndpoint;
        }

        private void OnConnect(ClientID _)
        {
            if (!IsLoaded && attach)
            {
                // lastEndpointData is stored via OnConnectedEndpoint before OnConnect hits
                var data = new CoherenceSceneData
                {
                    SceneName = sceneName,
                    ConnectionType = connectionType,
                    EndpointData = lastEndpointData,
                    LocalPhysicsMode = localPhysicsMode,
                };
                LoadingCoroutine = StartCoroutine(DoLoadScene(data));
            }
        }

        private void OnDisconnect(ConnectionCloseReason closeReason)
        {
            if (!IsLoaded && attach)
            {
                UnloadingCoroutine = StartCoroutine(DoUnloadScene());
            }
        }

        private void OnConnectionError(ConnectionException exception)
        {
            switch (exception)
            {
                case ConnectionDeniedException denyException:
                    logger.Error(Error.ToolkitSceneConnectionDenied,
                        ("Reason", denyException.CloseReason));

                    break;
                default:
                    logger.Error(Error.ToolkitSceneConnectionError,
                        ("exception", exception.Message));

                    break;
            }
        }

        private void OnConnectedEndpoint(EndpointData endpointData)
        {
            if (IsLoaded)
            {
                return;
            }

            lastEndpointData = endpointData;
        }

        private IEnumerator DoUnloadScene()
        {
            if (LoadingCoroutine != null)
            {
                yield return LoadingCoroutine;
            }

            ICoherenceBridge bridge = null;
            if (CoherenceScene.map.TryGetValue(scene, out CoherenceScene listener))
            {
                bridge = listener.bridge;
                bridge.Client.Disconnect();
            }

            onBeforeUnload.Invoke((CoherenceBridge)bridge);

            _ = loaderMap.Remove(scene);
            _ = dataMap.Remove(scene);
            _ = scenes.Remove(scene);

            if (scene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(scene, unloadSceneOptions);
            }
        }

        private IEnumerator DoLoadScene(CoherenceSceneData data)
        {
            loading = true;
            if (UnloadingCoroutine != null)
            {
                yield return UnloadingCoroutine;
            }

            loadOperations++;

            var idx = SceneManager.sceneCount;

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(sceneName, new LoadSceneParameters(LoadSceneMode.Additive, data.LocalPhysicsMode));
                op.allowSceneActivation = false;
            }
            catch
            {
                loadOperations--;
                loading = false;
                yield break;
            }

            while (!op.isDone)
            {
                if (op.progress >= 0.9f)
                {
                    var scene = SceneManager.GetSceneAt(idx);
                    this.scene = scene;
                    scenes.Add(scene);
                    dataMap.Add(scene, data);
                    loaderMap.Add(scene, this);
                    op.allowSceneActivation = true;

                    while (!op.isDone)
                    {
                        yield return null;
                    }
                }

                yield return null;
            }

            // by design https://issuetracker.unity3d.com/issues/loadsceneasync-allowsceneactivation-flag-is-ignored-in-awake
            //
            // I've observed scene activation takes up to 2 frames.
            // This doesn't seem to be documented anywhere, nor there's a clean way to know when activation has finished.
            // Scene activations happen synchronously, so we need to take into account other loads happening at the same time.
            // To achieve this, we keep a static counter of how many load operations we're performing (loadOperations).
            // NOTE if there are additional LoadSceneAsync operations in the same frame outside of this script,
            // waiting for activation can get messed up. To avoid this, we prioritize the execution of this script.

            for (int i = 0; i < loadOperations; i++)
            {
                yield return null;
                yield return null;
            }

            loadOperations--;

            if (!CoherenceScene.map.TryGetValue(scene, out CoherenceScene _))
            {
                var activeScene = SceneManager.GetActiveScene();
                if (SceneManager.SetActiveScene(scene))
                {
                    var go = new GameObject("coherence Scene Listener (Runtime)");
                    _ = go.AddComponent<CoherenceScene>();

                    logger.Warning(Warning.ToolkitSceneMissingScene,
                        ("scene", scene.name));

                    _ = SceneManager.SetActiveScene(activeScene);
                }
            }

            loading = false;
        }
    }
}
