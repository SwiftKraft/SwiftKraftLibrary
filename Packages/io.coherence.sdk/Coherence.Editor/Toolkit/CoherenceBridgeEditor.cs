// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System.Linq;
    using Cloud;
    using Coherence.Toolkit;
    using Connection;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(CoherenceBridge))]
    internal class CoherenceBridgeEditor : BaseEditor
    {
        private static class GUIContents
        {
            public static readonly GUIContent runtime = EditorGUIUtility.TrTextContent("Runtime Information");

            public static readonly GUIContent connected =
                EditorGUIUtility.TrTextContentWithIcon("Connected", Icons.GetPath("Coherence.Connected"));

            public static readonly GUIContent disconnected =
                EditorGUIUtility.TrTextContentWithIcon("Disconnected", Icons.GetPath("Coherence.Disconnected"));

            public static readonly GUIContent disconnect = EditorGUIUtility.TrTextContent("Disconnect");

            public static readonly GUIContent simulator = Icons.GetContent("Coherence.ConnectionType.Simulator",
                "Loaded through CoherenceSceneLoader with Simulator connection type.");

            public static readonly GUIContent client = Icons.GetContent("Coherence.ConnectionType.Client",
                "Loaded through CoherenceSceneLoader with Client connection type.");

            public static readonly GUIContent networkPrefix = EditorGUIUtility.TrTextContent("Remote Prefix",
                "Prefix to add to names of networked prefabs (CoherenceSync) instantiated by coherence.");

            public static readonly GUIContent autoConnectLabel = EditorGUIUtility.TrTextContent("Auto Connect",
                "None: Do not connect to coherence Cloud using any Player Account.\n\n" +
                "Main Player Account: Connect to coherence Cloud using the main player account (by default the first player account that logs in to coherence Cloud).\n\n" +
                "Auto Login As Guest: Automatically log in to coherence Cloud as a guest.\n\n");

            public static readonly GUIContent connectedToLabel = EditorGUIUtility.TrTextContent("Connected To",
                "The Player Account that this CoherenceBridge is currently connected to.");

            public static readonly GUIContent cloudUniqueId = EditorGUIUtility.TrTextContent("Cloud Unique ID",
                "(Optional) A locally unique identifier to associate with the guest player account.\n\n" +
                "If left blank, a Cloud Unique Id is generated automatically.\n\n" +
                "Cloud Unique Ids can be used to log into multiple different guest player accounts on the same device. " +
                "This might be useful for local multiplayer games, allowing each player to log into their own guest player account.");

            public static readonly GUIContent mainBridge = EditorGUIUtility.TrTextContent("Main Bridge");

            public static readonly GUIContent useBuildIndexAsId =
                EditorGUIUtility.TrTextContent("Use Build Index As Scene Id");

            public static readonly GUIContent sceneIndex = EditorGUIUtility.TrTextContent("Scene Identifier");

            public static readonly GUIContent connectionPrefabsTitle =
                EditorGUIUtility.TrTextContent("Connection Prefabs (Advanced)");

            public static readonly GUIContent generalSettings = EditorGUIUtility.TrTextContent("Settings");

            public static readonly GUIContent simulationFrameSettings =
                EditorGUIUtility.TrTextContent("Simulation Frame (Advanced)");

            public static readonly GUIContent
                playerAccountTitle = EditorGUIUtility.TrTextContent("Player Account");

            public static readonly GUIContent sceneTransitioningSettings =
                EditorGUIUtility.TrTextContent("Scene Transitioning");

            public static readonly GUIContent enableClientConnections = EditorGUIUtility.TrTextContent(
                $"{ObjectNames.NicifyVariableName(nameof(CoherenceBridge.EnableClientConnections))}",
                "With this enabled, you will be able to query how many users are connected via the CoherenceBridge.ClientConnections API.");

            public static readonly GUIContent createGlobalQuery = EditorGUIUtility.TrTextContent(
                $"{ObjectNames.NicifyVariableName(nameof(CoherenceBridge.CreateGlobalQuery))}",
                "Creates a Global Query. Required by Client Connections.\n\nWhen disabled, you will have to manually create a Global Query to see Client Connections. Recommended to disable only when there's a simulator or a host with Host Authority Restrictions.");

            public static readonly GUIContent controlTimeScale = EditorGUIUtility.TrTextContent("Control Time Scale",
                "Enables automatic client-server synchronization. Can be disabled temporarily for bullet time effects to intentionally desync clients for a short while. When set to true, Time.timeScale is nudged up/down so the game speed adapts to synchronize the game clock with the server clock.");

            public static readonly GUIContent adjustSimulationFrameByPing = EditorGUIUtility.TrTextContent(
                "Adjust Simulation Frame",
                "Adjusting the simulation frame by ping accounts for the time required for the packets to travel between the server and the client when calculating the client-server frame drift.");

            public static readonly GUIContent clientConnectionsHint =
                EditorGUIUtility.TrTextContent(
                    "Allows you to keep track of how many users are connected and uniquely identify them.");

            public static readonly GUIContent connectionPrefabsHint =
                EditorGUIUtility.TrTextContent("Instantiate this Prefab once per available Client Connection.");

            public static readonly GUIContent clientConnectionPrefab =
                EditorGUIUtility.TrTextContentWithIcon("Client", Icons.GetPath("Coherence.ConnectionType.Client"));

            public static readonly GUIContent simulatorConnectionPrefab =
                EditorGUIUtility.TrTextContentWithIcon("Simulator",
                    Icons.GetPath("Coherence.ConnectionType.Simulator"));

            public static readonly GUIContent events = EditorGUIUtility.TrTextContent("Events");

            public static readonly GUIContent liveQuerySynced =
                EditorGUIUtility.TrTextContent("Room/World state successfully synchronized.");

            public static readonly GUIContent onConnection =
                EditorGUIUtility.TrTextContent(
                    "Connection to Replication Server successful. Waiting for LiveQuery sync.");

            public static readonly GUIContent onDisconnection =
                EditorGUIUtility.TrTextContent("You were disconnected from the Room/World.");

            public static readonly GUIContent onConnectionError =
                EditorGUIUtility.TrTextContent(
                    "Connection failed. Check the ConnectionException parameter for a reason.");

            public static readonly GUIContent onNetworkedEntityCreated =
                EditorGUIUtility.TrTextContent("A new network entity is visible.");

            public static readonly GUIContent onNetworkedEntityDestroyed =
                EditorGUIUtility.TrTextContent("An existing network entity has been destroyed.");

            public static readonly GUIContent onConnectionValidation =
                EditorGUIUtility.TrTextContent("For simulators to handle incoming client connections.");

            public static readonly GUIContent clientConnections = EditorGUIUtility.TrTextContent("Client Connections");

            public static GUIContent FromConnectionType(ConnectionType type)
            {
                switch (type)
                {
                    case ConnectionType.Client:
                        return client;
                    case ConnectionType.Simulator:
                        return simulator;
                    case ConnectionType.Replicator:
                        break;
                }

                return GUIContent.none;
            }
        }

        private SerializedProperty controlTimeScale;
        private SerializedProperty enableClientConnections;
        private SerializedProperty createGlobalQuery;
        private SerializedProperty adjustSimulationFrameByPing;
        private SerializedProperty mainBridge;
        private SerializedProperty useBuildIndexAsId;
        private SerializedProperty sceneIdentifier;

        private SerializedProperty networkPrefix;
        private SerializedProperty uniqueId;
        private SerializedProperty autoLoginAsGuest;
        private SerializedProperty playerAccount;

        private SerializedProperty clientConnectionPrefab;
        private SerializedProperty simulatorConnectionPrefab;

        private SerializedProperty onConnected;
        private SerializedProperty onDisconnected;
        private SerializedProperty onConnectionError;
        private SerializedProperty onLiveQuerySynced;
        private SerializedProperty onNetworkedEntityCreated;
        private SerializedProperty onNetworkedEntityDestroyed;
        private SerializedProperty onConnectionValidation;

        private bool helpFoldout;
        private bool connectionPrefabsFoldout;

        protected override void OnEnable()
        {
            base.OnEnable();
            _ = Help.HasHelpForObject(this);

            networkPrefix = serializedObject.FindProperty(CoherenceBridge.Property.networkPrefix);
            uniqueId = serializedObject.FindProperty(CoherenceBridge.Property.uniqueId);
            autoLoginAsGuest = serializedObject.FindProperty(CoherenceBridge.Property.autoLoginAsGuest);
            playerAccount = serializedObject.FindProperty(CoherenceBridge.Property.playerAccount);

            controlTimeScale = serializedObject.FindProperty(nameof(CoherenceBridge.controlTimeScale));
            adjustSimulationFrameByPing =
                serializedObject.FindProperty(nameof(CoherenceBridge.adjustSimulationFrameByPing));
#pragma warning disable CS0618
            enableClientConnections = serializedObject.FindProperty(CoherenceBridge.Property.enableClientConnections);
#pragma warning restore CS0618
            mainBridge = serializedObject.FindProperty(CoherenceBridge.Property.mainBridge);
            sceneIdentifier = serializedObject.FindProperty(CoherenceBridge.Property.sceneIdentifier);
            useBuildIndexAsId = serializedObject.FindProperty(CoherenceBridge.Property.useBuildIndexAsId);
            createGlobalQuery = serializedObject.FindProperty(CoherenceBridge.Property.createGlobalQuery);

            clientConnectionPrefab = serializedObject.FindProperty(CoherenceBridge.Property.ClientConnectionEntry);
            simulatorConnectionPrefab = serializedObject.FindProperty(CoherenceBridge.Property.SimulatorConnectionEntry);

            onConnected = serializedObject.FindProperty(nameof(CoherenceBridge.onConnected));
            onDisconnected = serializedObject.FindProperty(nameof(CoherenceBridge.onDisconnected));
            onConnectionError = serializedObject.FindProperty(nameof(CoherenceBridge.onConnectionError));
            onLiveQuerySynced = serializedObject.FindProperty(nameof(CoherenceBridge.onLiveQuerySynced));
            onNetworkedEntityCreated = serializedObject.FindProperty(nameof(CoherenceBridge.onNetworkEntityCreated));
            onNetworkedEntityDestroyed = serializedObject.FindProperty(nameof(CoherenceBridge.onNetworkEntityDestroyed));
            onConnectionValidation = serializedObject.FindProperty(nameof(CoherenceBridge.onValidateConnectionRequest));
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            networkPrefix.Dispose();

            controlTimeScale.Dispose();
            enableClientConnections.Dispose();
            createGlobalQuery.Dispose();
            adjustSimulationFrameByPing.Dispose();
            mainBridge.Dispose();
            sceneIdentifier.Dispose();
            useBuildIndexAsId.Dispose();

            clientConnectionPrefab.Dispose();
            simulatorConnectionPrefab.Dispose();

            onConnected.Dispose();
            onDisconnected.Dispose();
            onConnectionError.Dispose();
            onLiveQuerySynced.Dispose();
            onNetworkedEntityCreated.Dispose();
            onNetworkedEntityDestroyed.Dispose();
            onConnectionValidation.Dispose();
        }

        private void DrawRuntime()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.LabelField(GUIContents.runtime, EditorStyles.boldLabel);
            _ = EditorGUILayout.BeginVertical(GUI.skin.box);
            var bridge = (CoherenceBridge)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (bridge.IsConnected)
                {
                    EditorGUILayout.PrefixLabel(GUIContents.connected, EditorStyles.miniButton);
                    EditorGUI.BeginDisabledGroup(!bridge.IsConnected);

                    if (GUILayout.Button(GUIContents.disconnect, EditorStyles.miniButton))
                    {
                        bridge.Disconnect();
                    }
                }
                else
                {
                    GUILayout.Label(GUIContents.disconnected);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Transport", EditorStyles.miniButton);
                EditorGUILayout.LabelField(bridge.Client?.DebugGetTransportDescription() ?? "Unknown");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Scene Name");
                EditorGUILayout.LabelField(bridge.InstantiationScene.HasValue
                    ? bridge.InstantiationScene.Value.name
                    : bridge.gameObject.scene.name);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Scene Identifier");
                EditorGUILayout.LabelField(bridge.ResolveSceneId().ToString());
            }

            if (bridge.IsConnected)
            {
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Client Connections");
                EditorGUI.indentLevel++;
                if (bridge.EnableClientConnections)
                {
                    var mine = bridge.ClientConnections.GetMine();
                    foreach (var conn in bridge.ClientConnections.GetAll().OrderBy(c => c.ClientId))
                    {
                        EditorGUILayout.LabelField(
                            EditorGUIUtility.TrTextContentWithIcon(conn.ClientId.ToString(),
                                GUIContents.FromConnectionType(conn.Type).image),
                            conn == mine ? EditorStyles.boldLabel : EditorStyles.label);
                    }
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("Global Query On is required for Client Connections.");
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField("Entities");
                foreach (var pair in bridge.EntitiesManager)
                {
                    _ = EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(pair.Key.ToString());
                    if (pair.Value != null && pair.Value.Sync != null)
                    {
                        if (GUILayout.Button(pair.Value.Sync.name) && pair.Value.Sync is Object obj)
                        {
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Missing / none.");
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            DrawSeparator();
        }

        private void DrawSettings()
        {
            EditorGUILayout.Separator();

            GUILayout.Label(GUIContents.generalSettings, EditorStyles.boldLabel);
            _ = EditorGUILayout.BeginVertical(GUI.skin.box);
            _ = EditorGUILayout.PropertyField(networkPrefix, GUIContents.networkPrefix);

            EditorGUILayout.Separator();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(GUIContents.playerAccountTitle, EditorStyles.boldLabel);
                ContentUtils.DrawHelpButton(DocumentationKeys.PlayerAccounts);
            }

            EditorGUI.indentLevel++;

            // In Play Mode display the Player Account that has been connected to the bridge.
            if (Application.isPlaying)
            {
                GUIContent playerAccountLabel;
                if (target is CoherenceBridge bridge
                    && PlayerAccount.Find(x => bridge.CloudServiceEquals(x.Services)) is { IsLoggedIn: true } connectedPlayerAccount)
                {
                    playerAccountLabel = new(connectedPlayerAccount.ToString());
                }
                else
                {
                    playerAccountLabel = new("None");
                }

                EditorGUILayout.LabelField(GUIContents.connectedToLabel, playerAccountLabel, EditorStyles.miniButton);
            }
            // In Edit Mode allow changing the login method / playerAccount to connect to at runtime.
            else
            {
                if (playerAccount.intValue == default)
                {
                    playerAccount.intValue = (int)(autoLoginAsGuest.boolValue ? CoherenceBridgePlayerAccount.AutoLoginAsGuest : CoherenceBridgePlayerAccount.None);
                }

                var valueWas = playerAccount.intValue;
                _ = EditorGUILayout.PropertyField(playerAccount, GUIContents.autoConnectLabel);
                if (valueWas != playerAccount.intValue)
                {
                    autoLoginAsGuest.boolValue = playerAccount.intValue == (int)CoherenceBridgePlayerAccount.AutoLoginAsGuest;
                }

                if (autoLoginAsGuest.boolValue || uniqueId.stringValue is { Length: > 0 })
                {
                    _ = EditorGUILayout.PropertyField(uniqueId, GUIContents.cloudUniqueId);
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Separator();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(GUIContents.simulationFrameSettings, EditorStyles.boldLabel);
                ContentUtils.DrawHelpButton(DocumentationKeys.SimFrame);
            }

            EditorGUI.indentLevel++;
            _ = EditorGUILayout.PropertyField(controlTimeScale, GUIContents.controlTimeScale);
            _ = EditorGUILayout.PropertyField(adjustSimulationFrameByPing, GUIContents.adjustSimulationFrameByPing);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private void DrawClientConnectionSettings()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GUIContents.clientConnections, EditorStyles.boldLabel);
            ContentUtils.DrawHelpButton(DocumentationKeys.ClientMessages);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label(GUIContents.clientConnectionsHint, ContentUtils.GUIStyles.miniLabel);

            _ = EditorGUILayout.BeginVertical(GUI.skin.box);

            _ = EditorGUILayout.PropertyField(enableClientConnections, GUIContents.enableClientConnections);

            if (enableClientConnections.boolValue)
            {
                EditorGUI.indentLevel++;
                _ = EditorGUILayout.PropertyField(createGlobalQuery, GUIContents.createGlobalQuery);
                EditorGUI.indentLevel--;

                EditorGUILayout.Separator();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(GUIContents.connectionPrefabsTitle, EditorStyles.boldLabel);
                ContentUtils.DrawHelpButton(DocumentationKeys.ClientConnectionPrefabs);
                EditorGUILayout.EndHorizontal();
                GUILayout.Label(GUIContents.connectionPrefabsHint, ContentUtils.GUIStyles.miniLabel);

                _ = EditorGUILayout.PropertyField(clientConnectionPrefab, GUIContents.clientConnectionPrefab);

                HandleChangeClientConnectionAuthorityType(clientConnectionPrefab, "coherence.AskedToUpdateClientConnectionTransferType");

                _ = EditorGUILayout.PropertyField(simulatorConnectionPrefab, GUIContents.simulatorConnectionPrefab);

                HandleChangeClientConnectionAuthorityType(simulatorConnectionPrefab, "coherence.AskedToUpdateSimulatorConnectionTransferType");

                EditorGUILayout.Separator();
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GUIContents.sceneTransitioningSettings, EditorStyles.boldLabel);
                    ContentUtils.DrawHelpButton(DocumentationKeys.SceneTransitioning);
                }

                EditorGUI.indentLevel++;
                _ = EditorGUILayout.PropertyField(mainBridge, GUIContents.mainBridge);
                _ = EditorGUILayout.PropertyField(useBuildIndexAsId, GUIContents.useBuildIndexAsId);

                if (!useBuildIndexAsId.boolValue)
                {
                    _ = EditorGUILayout.PropertyField(sceneIdentifier, GUIContents.sceneIndex);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            static void HandleChangeClientConnectionAuthorityType(SerializedProperty serializedProperty, string userWasPromptedToUpdateSessionStateKey)
            {
                if (serializedProperty.objectReferenceValue is not CoherenceSyncConfig clientConnectionPrefabValue
                    || !clientConnectionPrefabValue
                    || clientConnectionPrefabValue.Sync is not { } clientConnectionPrefabSync
                    || !clientConnectionPrefabSync
                    || clientConnectionPrefabValue.Sync.AuthorityTransfer is CoherenceSync.AuthorityTransferType.NotTransferable)
                {
                    return;
                }

                const string errorMessage = "Connection prefabs must have 'Authority Type' set to '" +
                                            nameof(CoherenceSync.AuthorityTransferType.NotTransferable) + "'.";

                var warningIconContent = EditorGUIUtility.IconContent("Warning");
                var warningContent = new GUIContent(errorMessage, warningIconContent.image);
                EditorGUILayout.LabelField(warningContent, CoherenceHubLayout.Styles.Label);

                if (GUILayout.Button(new GUIContent("Fix Authority Type"), ContentUtils.GUIStyles.bigButton))
                {
                    using var clientConnectionPrefabSyncSerializedObject = new SerializedObject(clientConnectionPrefabSync);
                    using var authorityTransferConfigProperty = clientConnectionPrefabSyncSerializedObject.FindProperty(nameof(CoherenceSync.authorityTransferConfig));
                    authorityTransferConfigProperty.intValue = (int)CoherenceSync.AuthorityTransferConfig.NotTransferable;
                    _ = clientConnectionPrefabSyncSerializedObject.ApplyModifiedProperties();

                    var parentObject = clientConnectionPrefabSync.gameObject;
                    var parentAsset = AssetDatabase.GetAssetPath(parentObject);
                    if (string.IsNullOrEmpty(parentAsset))
                    {
                        return;
                    }

                    var guid = new GUID(AssetDatabase.AssetPathToGUID(parentAsset));
                    AssetDatabase.SaveAssetIfDirty(guid);
                    BakeUtil.CoherenceSyncSchemasDirty = true;
                }
            }
        }

        private void DrawEvents()
        {
            onConnected.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(onConnected.isExpanded, GUIContents.events);
            if (onConnected.isExpanded)
            {
                EditorGUILayout.Separator();
                DrawEvent(GUIContents.liveQuerySynced, onLiveQuerySynced, DocumentationLinks.GetDocsUrl(DocumentationKeys.OnLiveQuerySynced));
                DrawEvent(GUIContents.onConnection, onConnected);
                DrawEvent(GUIContents.onDisconnection, onDisconnected);
                DrawEvent(GUIContents.onConnectionError, onConnectionError);
                DrawEvent(GUIContents.onNetworkedEntityCreated, onNetworkedEntityCreated);
                DrawEvent(GUIContents.onNetworkedEntityDestroyed, onNetworkedEntityDestroyed);
                DrawEvent(GUIContents.onConnectionValidation, onConnectionValidation, DocumentationLinks.GetDocsUrl(DocumentationKeys.ConnectionValidation));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawEvent(GUIContent explanation, SerializedProperty property, string link = null)
        {
            if (string.IsNullOrEmpty(link))
            {
                EditorGUILayout.LabelField(explanation);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(explanation, ContentUtils.GUIStyles.labelNoExpand);
                EditorGUILayout.Space();
                ContentUtils.DrawHelpButton(link);
                EditorGUILayout.EndHorizontal();
            }

            _ = EditorGUILayout.PropertyField(property);
        }

        protected override void OnGUI()
        {
            DrawRuntime();

            serializedObject.Update();

            helpFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(helpFoldout, "What is the CoherenceBridge");
            if (helpFoldout)
            {
                CoherenceHubLayout.DrawInfoLabel(
                    "The CoherenceBridge establishes a connection between your scene and the coherence Replication Server. It makes sure all networked entities stay in sync.");
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            DrawEvents();
            DrawSeparator();
            DrawClientConnectionSettings();
            DrawSettings();

            _ = serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Separator();
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(new Rect(rect)
            {
                x = 0f,
                width = Screen.width
            }, Color.black);
            EditorGUILayout.Separator();
        }
    }
}
