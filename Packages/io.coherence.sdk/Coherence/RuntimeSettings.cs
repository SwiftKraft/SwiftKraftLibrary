// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using Common;
    using System;
    using Transport;
    using UnityEngine;
    using UnityEngine.Serialization;
    using System.Collections.Generic;
    using System.Linq;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [PreloadedSingleton]
    public class RuntimeSettings : PreloadedSingleton<RuntimeSettings>, IRuntimeSettings
    {
        public bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;
        public string ApiEndpoint => playApiEndpoint;
        public string WebSocketEndpoint => webSocketEndpoint;
        public string LocalHost => localHost;
        public int LocalWorldUDPPort => localWorldUDPPort;
        public int LocalWorldWebPort => localWorldWebPort;
        public int RemoteWebPort => remoteWebPort;
        public int LocalRoomsUDPPort => localRoomsUDPPort;
        public int LocalRoomsWebPort => localRoomsWebPort;
        public int APIPort => apiPort;
        public int WorldsAPIPort => worldsAPIPort;
        public string LocalHttpServerHost => localHttpServerHost;
        public int LocalHttpServerPort => localHttpServerPort;
        public bool LocalDevelopmentMode => localDevelopmentMode;
        public bool UseDebugStreams => useDebugStreams;
        public AdvancedSettings Advanced => advancedSettings;

        public string RuntimeKey
        {
            get => runtimeKey;
            internal set => runtimeKey = value;
        }

        public string SimulatorSlug
        {
            get => simulatorSlug;
            set => simulatorSlug = value;
        }

        public string SchemaID
        {
            get => schemaID;
            internal set => schemaID = value;
        }

        public IVersionInfo VersionInfo
        {
            get => versionInfo;
            set => versionInfo = (VersionInfo)value;
        }

        public string OrganizationID
        {
            get => organizationID;
            internal set => organizationID = value;
        }

        public string OrganizationName
        {
            get => organizationName;
            internal set => organizationName = value;
        }

        public string ProjectID
        {
            get => projectID;
            internal set => projectID = value;
        }

        public string ProjectName
        {
            get => projectName;
            internal set => projectName = value;
        }

        public TransportType TransportType
        {
            get => transportType;
            internal set => transportType = value;
        }

        public TransportConfiguration TransportConfiguration
        {
            get => transportConfiguration;
            internal set => transportConfiguration = value;
        }

        public string ReplicationServerToken
        {
            get => replicationServerToken;
            internal set => replicationServerToken = value;
        }

        public IReadOnlyCollection<SchemaAsset> DefaultSchemas
        {
            get => schemas;
        }

        public string CombinedSchemaText { get; private set; }

        public bool DisableKeepAlive
        {
            get { return disableKeepAlive; }
            set { disableKeepAlive = value; }
        }

        [SerializeField] private string schemaID;
        [SerializeField] private VersionInfo versionInfo;
        [SerializeField] private string runtimeKey;
        [SerializeField] private string simulatorSlug;
        [SerializeField] private string localHost = Constants.localHost;
        [SerializeField, FormerlySerializedAs("localPort")]
        private int localWorldUDPPort = Constants.localWorldUDPPort;
        [SerializeField] private int localWorldWebPort = Constants.localWorldWebPort;
        [SerializeField] private int remoteWebPort = Constants.remoteWebPort;
        [SerializeField] private int localRoomsUDPPort = Constants.localRoomsUDPPort;
        [SerializeField] private int localRoomsWebPort = Constants.localRoomsWebPort;
        [SerializeField, FormerlySerializedAs("roomsPort")]
        private int apiPort = Constants.apiPort;
        [SerializeField]
        private int worldsAPIPort = Constants.worldsApiPort;
        [SerializeField, Tooltip("Can be overriden via --coherence-multi-room-sim-host")]
        private string localHttpServerHost = Constants.localHttpServerHost;
        [SerializeField, Tooltip("Can be overriden via --coherence-multi-room-sim-port")]
        private int localHttpServerPort = Constants.localHttpServerPort;
        [FormerlySerializedAs("allowLocal")]
        [SerializeField] private bool localDevelopmentMode = true;
        [SerializeField] private bool useDebugStreams = false;
        [SerializeField] private string organizationID;
        [SerializeField] private string projectID;
        [SerializeField] private string projectName;
        [SerializeField] private string organizationName;
        [SerializeField] private TransportType transportType;
        [SerializeField] private TransportConfiguration transportConfiguration;
        [SerializeField] private AdvancedSettings advancedSettings;

        [SerializeField] private string replicationServerToken;

        [SerializeField, Tooltip("Can be overriden via --coherence-play-api-endpoint")]
        private string playApiEndpoint = Constants.apiEndpoint;

        [Tooltip("Generated from the API Endpoint")]
        private string webSocketEndpoint;

#if COHERENCE_FEATURE_NATIVE_CORE
        [SerializeField] private bool useNativeCore = false;

        public bool UseNativeCore
        {
            get => useNativeCore;
        }
#endif

        [SerializeField]
        internal SchemaAsset[] schemas;

        [SerializeField]
        public SchemaAsset[] extraSchemas;

        [Obsolete("Use TransportType instead.")]
        [Deprecated("04/2024", 1, 3, 0, Reason = "Use TransportType instead.")]
        [SerializeField, HideInInspector]
        internal DefaultTransportMode defaultTransportMode;

        // Used to mark if defaultTransportMode was migrated or not
        [SerializeField, HideInInspector]
        internal bool defaultTransportModeMigrated;

        // This should be of type Toolkit.CoherenceSync.AuthorityTransferType but we can't reference
        // Toolkit assembly from this assembly. So we just store it as an int :(
        // Default value of 2 is AuthorityTransferType.Steal
        [SerializeField]
        internal int defaultAuthorityTransferType = 2;

        [NonSerialized] private bool disableKeepAlive = false;

        private void Reset()
        {
            localHost = Constants.localHost;
            localWorldUDPPort = Constants.localWorldUDPPort;
            localWorldWebPort = Constants.localWorldWebPort;
            localRoomsUDPPort = Constants.localRoomsUDPPort;
            localRoomsWebPort = Constants.localRoomsWebPort;
            apiPort = Constants.apiPort;
            remoteWebPort = Constants.remoteWebPort;
            playApiEndpoint = Constants.apiEndpoint;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!this)
            {
                return;
            }

#if UNITY_EDITOR
            hideFlags &= ~HideFlags.NotEditable;
#endif

            LoadCliOverrides();
            Init();
        }

        private void Init()
        {
            if (string.IsNullOrEmpty(playApiEndpoint))
            {
                playApiEndpoint = Constants.apiEndpoint;
            }
            else if (!playApiEndpoint.StartsWith("http://localhost:"))
            {
                playApiEndpoint = playApiEndpoint.Replace("http://", "https://");
            }

            webSocketEndpoint = playApiEndpoint.
                Replace("http://", "ws://").
                Replace("https://", "wss://");

            if (webSocketEndpoint.IndexOf("/play/api/v1", StringComparison.Ordinal) > -1)
            {
                // OBSOLETE (not used in 0.9)
                webSocketEndpoint = webSocketEndpoint.Replace("/play/api/v1", "/play/ws/v1");
            }
            else
            {
                webSocketEndpoint = webSocketEndpoint + "/ws";
            }

            CombinedSchemaText = schemas != null ? string.Join("\n", schemas.Select(s => s.raw)) : "";
        }

        private static bool TryGetArg(string name, out string value)
        {
            var commandLineArgs = Environment.GetCommandLineArgs();
            var idx = Array.IndexOf(commandLineArgs, name);

            if (idx != -1 && commandLineArgs.Length > idx + 1)
            {
                value = commandLineArgs[idx + 1];
                return true;
            }
            else
            {
                value = string.Empty;
                return false;
            }
        }

        private void LoadCliOverrides()
        {
            if (TryGetArg("--coherence-play-api-endpoint", out var apiEndpoint))
            {
                playApiEndpoint = apiEndpoint;
            }
            else if (TryGetArg("--coherence-play-endpoint", out apiEndpoint))
            {
                playApiEndpoint = apiEndpoint + "/api/v1";
            }

            // legacy, kept for backwards compatibility. Use "--coherence-multi-room-sim-port"
            if (TryGetArg("--coherence-http-server-port", out var portString) && int.TryParse(portString, out var port))
            {
                localHttpServerPort = port;
            }

            if (TryGetArg("--coherence-multi-room-sim-port", out portString) && int.TryParse(portString, out port))
            {
                localHttpServerPort = port;
            }

            if (TryGetArg("--coherence-multi-room-sim-host", out var host))
            {
                localHttpServerHost = host;
            }

#if COHERENCE_FEATURE_NATIVE_CORE
            if (TryGetArg("--coherence-use-native-core", out var use))
            {
                useNativeCore = use.ToLower() == "true";
            }
#endif
        }

        public void SetApiEndpoint(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                playApiEndpoint = Constants.apiEndpoint;
                return;
            }

            playApiEndpoint = endpoint;
            Init();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        public void SetRuntimeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            runtimeKey = key;
            Init();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        public void SetProjectID(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            projectID = id;
            Init();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        public void SetSchemaID(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            schemaID = id;
            Init();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        public void SetApiPort(int port)
        {
            if (port == 0)
            {
                apiPort = Constants.apiPort;
                return;
            }

            apiPort = port;
            Init();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        [Serializable]
        public class AdvancedSettings
        {
            internal static bool Enabled = Plugins.NativeUtils.ThreadResumerSettings.SteamDetected;
            public Plugins.NativeUtils.ThreadResumerSettings ThreadResumer = new();
        }
    }
}
