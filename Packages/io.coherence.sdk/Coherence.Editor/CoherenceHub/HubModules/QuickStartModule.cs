// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Toolkit;
    using Coherence.Toolkit;
    using UI;
    using UnityEditor;
    using UnityEngine;

    [HubModule(Priority = 95)]
    public class QuickStartModule : HubModule
    {
        /// <summary>
        /// GUID for the rooms connection dialog sample asset
        /// </summary>
        /// <remarks>
        /// Instead of finding it through the project, or storing a path, we use the GUID which it's meant to persist,
        /// even if the file is relocated elsewhere.
        /// </remarks>
        private string dialogAssetGuid = "a3c758d9a9a540242b3e7c45a40460f1";
        private bool hasBridge;
        private bool hasQuery;

        public override string ModuleName => "Quick Start";

        private class ModuleGUIContents
        {
            public static readonly GUIContent HelpTitle = new("How to start networking?");
            public static readonly GUIContent HelpContent = new($"The scene you want to networks needs to have a {nameof(CoherenceBridge)} and a {nameof(CoherenceLiveQuery)}.\n\nThen, you need to script your connection logic, or use any of our development-ready connection dialog.");
            public static readonly GUIContent OnlineResources = EditorGUIUtility.TrTextContent("Online Resources");
            public static readonly GUIContent SetupAProject = EditorGUIUtility.TrTextContent("Setup a Project");
            public static readonly GUIContent ConnectToSelfHostedRs = EditorGUIUtility.TrTextContent("Connect to Self-Hosted Replication Server");
            public static readonly GUIContent ConnectToCloudRs = EditorGUIUtility.TrTextContent("Connect to Cloud Replication Server");

            public static readonly GUIContent BridgeCreate = EditorGUIUtility.TrTextContent($"Create {nameof(CoherenceBridge)} on scene");
            public static readonly GUIContent QueryCreate = EditorGUIUtility.TrTextContent($"Create {nameof(CoherenceLiveQuery)} on scene");
            public static readonly GUIContent BridgeCreated = EditorGUIUtility.TrTextContentWithIcon($"{nameof(CoherenceBridge)} on scene", "Installed");
            public static readonly GUIContent QueryCreated = EditorGUIUtility.TrTextContentWithIcon($"{nameof(CoherenceLiveQuery)} on scene", "Installed");
        }

        public override HelpSection Help => new()
        {
            title = ModuleGUIContents.HelpTitle,
            content = ModuleGUIContents.HelpContent,
        };

        protected override void OnEnable()
        {
            base.OnEnable();
            CheckRequiredComponents();
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            CheckRequiredComponents();
            Repaint();
        }

        private void CheckRequiredComponents()
        {
            hasBridge = FindAnyObjectByType<CoherenceBridge>();
            hasQuery = FindAnyObjectByType<CoherenceLiveQuery>();
        }

        public override void OnGUI()
        {
            CoherenceHubLayout.DrawSection("Required Components", DrawRequiredComponents);
            CoherenceHubLayout.DrawSection("Establish a Connection", DrawEstablishConnection);
            CoherenceHubLayout.DrawSection("Network your GameObjects", DrawNetworkGameObjects);
            CoherenceHubLayout.DrawSection(ModuleGUIContents.OnlineResources, DrawLinks);
        }

        private void DrawRequiredComponents()
        {
            GUILayout.Label("Your game needs two components to exist on the scene you want to network: CoherenceBridge and CoherenceLiveQuery.\nThe bridge helps establish the connection and keep track of entities, and the live query helps define the area of interest to network.", ContentUtils.GUIStyles.wrappedLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(hasBridge);
            if (GUILayout.Button(hasBridge ? ModuleGUIContents.BridgeCreated : ModuleGUIContents.BridgeCreate, ContentUtils.GUIStyles.bigButton))
            {
                var go = Utils.CreateInstance<CoherenceBridge>(nameof(CoherenceBridge));
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(hasQuery);
            if (GUILayout.Button(hasQuery ? ModuleGUIContents.QueryCreated : ModuleGUIContents.QueryCreate, ContentUtils.GUIStyles.bigButton))
            {
                var go = Utils.CreateInstance<CoherenceLiveQuery>(nameof(CoherenceLiveQuery));
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawEstablishConnection()
        {
            GUILayout.Label(
                "With the former components in place, you need to establish a connection against the Replication Server (be it local or in the Cloud)." +
                "\n\nWe offer connection dialogs that you can use right away, or base your work off.",
                ContentUtils.GUIStyles.wrappedLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Import Connect Dialog", ContentUtils.GUIStyles.bigButton))
            {
                var path = AssetDatabase.GUIDToAssetPath(dialogAssetGuid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError("Can't find connect dialog asset for asset with GUID " + dialogAssetGuid);
                    GUIUtility.ExitGUI();
                }

                var asset = AssetDatabase.LoadAssetAtPath<SampleDialogAsset>(path);

                if (!asset)
                {
                    Debug.LogError("Can't find connect dialog asset for asset with path " + path);
                    GUIUtility.ExitGUI();
                }
                UIUtils.ImportAndPingFromPackageSample(asset, null);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Alternatively, you can learn more on our documentation site on how to create your own connection logic. Check the Online Resources section down below for examples and documentation.",
                ContentUtils.GUIStyles.wrappedLabel);
        }

        private void DrawNetworkGameObjects()
        {
            GUILayout.Label($"Prefabs having a {nameof(CoherenceSync)} component attached are networked.", ContentUtils.GUIStyles.wrappedLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Go to 'Networked Prefabs' tab", ContentUtils.GUIStyles.bigButton))
            {
                _ = CoherenceHub.Open<NetworkedPrefabsModule>();
            }

        }

        private void DrawLinks()
        {
            CoherenceHubLayout.DrawLink(ModuleGUIContents.SetupAProject, DocumentationLinks.GetDocsUrl(DocumentationKeys.ProjectSetup));
            CoherenceHubLayout.DrawLink(ModuleGUIContents.ConnectToCloudRs, DocumentationLinks.GetDocsUrl(DocumentationKeys.CloudApi));
            CoherenceHubLayout.DrawLink(ModuleGUIContents.ConnectToSelfHostedRs, DocumentationLinks.GetDocsUrl(DocumentationKeys.ReplicationServerApi));
        }
    }
}
