// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using Portal;
    using ReplicationServer;
    using Toolkit;
    using UI;
    using UnityEditor;
    using UnityEngine;

    internal static class CoherenceMainMenu
    {
        // Order of sections. Any separation of more than 10 creates a separator
        private const int Section1 = 1;
        private const int Section2 = 100;
        private const int Section3 = 200;
        private const int Section4 = 300;
        private const int Section5 = 400;

        public const string BackupWorldDataMenuItem = "coherence/Local Replication Server/Backup World Data";

        private static void Analytic(string menuItem)
        {
            Analytics.Capture(Analytics.Events.MenuItem,("menu", "main"), ("item", menuItem));
        }

        [MenuItem("coherence/Hub", false, Section1 + 1)]
        public static void OpenCoherenceHub()
        {
            Analytic("hub");
            CoherenceHub.Open();
        }

        [MenuItem("coherence/Networked Prefabs", false, Section1 + 52)]
        public static void OpenNetworkedPrefabs()
        {
            Analytic("csync_objects");
            CoherenceSyncObjectsStandaloneWindow.Open();
        }

        [MenuItem("coherence/Online Dashboard", false, Section1 + 53)]
        public static void DeveloperPortal()
        {
            Analytic("online_dashboard");
            UsefulLinks.DeveloperPortal();
        }


        [MenuItem("coherence/Explore Samples", false, Section1 + 54)]
        internal static void ShowAddDialogWindow(MenuCommand menuCommand)
        {
            Analytic("explore_samples");
            SampleDialogPickerWindow.ShowWindow(parentGameObject: menuCommand.context as GameObject);
        }

        // Settings
        [MenuItem("coherence/Settings", false, Section2)]
        public static void OpenProjectSettings()
        {
            Analytic("settings");
            _ = SettingsService.OpenProjectSettings(Paths.projectSettingsWindowPath);
        }

        // Scene Setup
        [MenuItem("coherence/Scene Setup/Create CoherenceBridge", false, Section2 + 1)]
        internal static void AddBridge(MenuCommand menuCommand)
        {
            Analytic("scene_create_bridge");
            Utils.AddBridgeInstanceInScene(menuCommand);
        }

        [MenuItem("coherence/Scene Setup/Create LiveQuery", false, Section2 + 2)]
        internal static void AddLiveQuery(MenuCommand menuCommand)
        {
            Analytic("scene_create_query");
            Utils.AddLiveQueryInstanceInScene(menuCommand);
        }

        // GameObject Setup
        [MenuItem("coherence/GameObject Setup/Add CoherenceSync", true, Section2 + 4)]
        public static bool CanAddCoherenceSync()
        {
            return GameObjectSetup.ObjectHasNoSync();
        }

        [MenuItem("coherence/GameObject Setup/Add CoherenceSync", false, Section2 + 4)]
        public static void AddCoherenceSync()
        {
            Analytic("gameobject_add_sync");
            GameObjectSetup.AddCoherenceSync();
        }

        [MenuItem("coherence/GameObject Setup/Configure", false, Section2 + 5)]
        public static void OpenSelectWindow()
        {
            Analytic("gameobject_configure");
            _ = CoherenceSyncBindingsWindow.GetWindow();
        }

        [MenuItem("coherence/GameObject Setup/Optimize", false, Section2 + 6)]
        public static void OpenBindingsWindow()
        {
            Analytic("gameobject_optimize");
            BindingsWindow.Init();
        }

        // Servers
        [MenuItem("coherence/Local Replication Server/Run for Rooms %#&r", false, Section3)]
        public static void RunRoomsReplicationServerInTerminal()
        {
            Analytic("run_local_rooms");
            EditorLauncher.RunRoomsReplicationServerInTerminal();
        }

        [MenuItem("coherence/Local Replication Server/Run for Worlds %#&w", false, Section3 + 1)]
        public static void RunWorldsReplicationServerInTerminal()
        {
            Analytic("run_local_worlds");
            EditorLauncher.RunWorldsReplicationServerInTerminal();
        }

        [MenuItem(BackupWorldDataMenuItem, true, Section3 + 2)]
        private static bool BackupWorldDataValidate()
        {
            return !CloneMode.Enabled;
        }

        [MenuItem("coherence/Local Replication Server/Open in coherence Hub", false, Section3 + 3)]
        public static void OpenReplicationServerWindow()
        {
            Analytic("run_local_hub");
            CoherenceHub.Open();
            CoherenceHub.FocusModule<ReplicationServerModule>();
        }

        // Baking and schemas
        [MenuItem("coherence/Bake %#&m", true, Section3 + 1)]
        private static bool BakeValidate()
        {
            return !CloneMode.Enabled;
        }

        [MenuItem("coherence/Bake %#&m", false, Section3 + 1)]
        public static void BakeSchemas()
        {
            Analytic("bake");
            BakeUtil.Bake();
        }

        [MenuItem("coherence/Upload Schemas", false, Section3 + 2)]
        public static void UploadSchemas()
        {
            Analytic("upload_schemas");
            _ = Schemas.UploadActive(InteractionMode.UserAction);
        }

        [MenuItem("coherence/Upload Schemas", true, Section3 + 2)]
        public static bool UploadSchemasValidate()
        {
            return !string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID);
        }

        // Game
        [MenuItem("coherence/Share/Build Upload", false, Section3 + 3)]
        public static void BuildGameWizard()
        {
            Analytic("share_build");
            GameBuildWindow.Init();
        }

#if COHERENCE_ENABLE_MULTI_ROOM_SIMULATOR
        // Simulators
        [MenuItem("coherence/Simulator/Multi-Room Simulator Wizard", false, Section3 + 4)]
        public static void OpenMrsWizard()
        {
            Analytic("mrs_wizard");
            MultiRoomSimulatorsWizardModuleWindow.OpenWindow(true);
        }
#endif

        [MenuItem("coherence/Simulator/Simulator Build Wizard", false, Section3 + 5)]
        public static void OpenSimulatorWindow()
        {
            Analytic("sim_wizard");
            SimulatorWindow.Init();
        }

        [MenuItem("coherence/Simulator/Run Simulator Locally", true, Section3 + 6)]
        public static bool CanRunLocalSimulator()
        {
            return SimulatorEditorUtility.CanRunLocalSimulator();
        }

        [MenuItem("coherence/Simulator/Run Simulator Locally", false, Section3 + 7)]
        public static void RunLocalSimulator()
        {
            Analytic("sim_run_local");
            SimulatorEditorUtility.RunLocalSimulator();
        }

        [MenuItem("coherence/Simulator/Add AutoSimulatorConnection", false, Section3 + 7)]
        public static void AddAutoSimulatorConnection(MenuCommand menuCommand)
        {
            Utils.AddAutoSimulatorConnection(menuCommand);
        }


        [MenuItem("Assets/Migrate coherence Assets", true, 40), MenuItem("coherence/Migrate coherence Assets", true, Section4),]
        private static bool MenuMigrationValidate()
        {
            return !CloneMode.Enabled;
        }

        [MenuItem("Assets/Migrate coherence Assets", false, 40), MenuItem("coherence/Migrate coherence Assets", false, Section4),]
        public static void MenuMigration()
        {
            Analytic("migrate_assets");
            _ = Migration.Migrate();
        }

        [MenuItem("coherence/Update Bindings", true, Section4 + 1)]
        private static bool UpdateBindingsValidate()
        {
            return !CloneMode.Enabled;
        }

        [MenuItem("coherence/Update Bindings", false, Section4 + 1)]
        public static void UpdateBindings()
        {
            Analytic("update_bindings");
            EditorCache.UpdateBindingsAndNotify();
        }

        // Links
        [MenuItem("coherence/Documentation", false, Section5)]
        public static void OpenDocumentation()
        {
            Analytic("open_docs");
            UsefulLinks.Documentation();
        }

        [MenuItem("coherence/Help/Community Forums", false, Section5 + 1)]
        public static void OpenCommunityForums()
        {
            Analytic("help_community");
            UsefulLinks.CommunityForum();
        }

        [MenuItem("coherence/Help/Discord", false, Section5 + 2)]
        public static void OpenDiscord()
        {
            Analytic("help_discord");
            UsefulLinks.Discord();
        }

        [MenuItem("coherence/Help/Support", false, Section5 + 2)]
        public static void OpenSupport()
        {
            Analytic("help_support");
            UsefulLinks.Support();
        }

        [MenuItem("coherence/Help/Report a Bug...", false, Section5 + 3)]
        private static void ReportABug()
        {
            Analytic("help_report_bug");
            BugReportHelper.DisplayReportBugDialogs();
        }

    }
}
