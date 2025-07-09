// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using Coherence.Toolkit.ReplicationServer;
    using Features;
    using Portal;
    using ReplicationServer;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [HubModule(Priority = 80)]
    public class ReplicationServerModule : HubModule
    {
        public override string ModuleName => "Replication Server";

        public static class ModuleGUIContents
        {
            public static readonly GUIContent Rooms = EditorGUIUtility.TrTextContentWithIcon("Run for Rooms",
                "Start a terminal with a Replication Server for Rooms.",
                Icons.GetPath("Coherence.Terminal"));

            public static readonly GUIContent Worlds = EditorGUIUtility.TrTextContentWithIcon("Run for Worlds",
                "Start a terminal with a Replication Server for Worlds.",
                Icons.GetPath("Coherence.Terminal"));
            public static readonly GUIContent WhatAreReplicationServers = new("In order for clients to communicate with each other, they need a replication server. A replication server can either run locally or in the cloud. The responsibility of the server is to replicate the state of the world across the network." +
                                                               "\nIf a new schema has been created, you also need to restart the replication server.");
        }

        public override HelpSection Help => new()
        {
            title = new GUIContent("What are Replication Servers?"),
            content = ModuleGUIContents.WhatAreReplicationServers,
        };

        public override void OnModuleEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            Refresh();
        }

        public override void OnModuleDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void Refresh()
        {
            if (!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken))
            {
                PortalLogin.FetchOrgs();
            }
        }

        private void OnProjectChanged()
        {
            Refresh();
        }

        public override void OnGUI()
        {
            CoherenceHubLayout.DrawSection("Replication Server", DrawReplicationServers);
            CoherenceHubLayout.DrawSection("Online Resources", DrawLinks);
        }

        private void DrawReplicationServers()
        {
            if (BakeUtil.Outdated)
            {
                EditorGUILayout.HelpBox("Bake is outdated. It is recommended to bake before you start a Replication Server.", MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ModuleGUIContents.Rooms, ContentUtils.GUIStyles.bigButton))
                {
                    EditorLauncher.RunRoomsReplicationServerInTerminal();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(ContentUtils.GUIContents.clipboard, ContentUtils.GUIStyles.iconButton))
                {
                    var command = Launcher.ToCommand(EditorLauncher.CreateLocalRoomsConfig());
                    GUIUtility.systemCopyBuffer = command;
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent("Command copied to clipboard"));
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ModuleGUIContents.Worlds, ContentUtils.GUIStyles.bigButton))
                {
                    EditorLauncher.RunWorldsReplicationServerInTerminal();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(ContentUtils.GUIContents.clipboard, ContentUtils.GUIStyles.iconButton))
                {
                    var command = Launcher.ToCommand(EditorLauncher.CreateLocalWorldConfig());
                    GUIUtility.systemCopyBuffer = command;
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent("Command copied to clipboard"));
                }
            }
        }

        public void DrawLinks()
        {
            CoherenceHubLayout.DrawLink(new GUIContent("Testing Multiplayer Locally"), DocumentationLinks.GetDocsUrl(DocumentationKeys.LocalServers));
        }
    }
}
