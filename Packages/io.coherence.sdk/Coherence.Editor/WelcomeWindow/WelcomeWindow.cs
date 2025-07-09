namespace Coherence.Editor
{
    using System.Collections.Generic;
    using UI;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class WelcomeWindow : EditorWindow
    {
        private static class GUIContents
        {
            public static readonly GUIContent Title = Icons.GetContentWithText("EditorWindow", "Welcome to coherence");
            public static readonly GUIContent UpdateTitle = Icons.GetContentWithText("EditorWindow", "coherence updated");
        }

        private struct UpdateInfo
        {
            public string Version;
            public string OldVersion;
            public HashSet<string> MigrationMessages;

            public bool IsActive => OldVersion != null && Version != null;
        }

        private class WelcomeEventProperties : Analytics.BaseProperties
        {
            public string button_name;
            public bool comes_from_update;
        }

        private static VisualElement root;

        private UpdateInfo update;

        // This is provided as a default reference on the .cs file itself, in the import settings in the Editor
        [SerializeField] private SampleDialogAsset _sampleScenesAsset;

        public static WelcomeWindow Open()
        {
            return GetWindow<WelcomeWindow>();
        }

        private void OnEnable()
        {
            minSize = maxSize = new Vector2(570, 526);
        }

        public void SetUpdateInfo(string oldVersion, string version, HashSet<string> migrationMessages)
        {
            update = new UpdateInfo
            {
                Version = version,
                OldVersion = oldVersion,
                MigrationMessages = migrationMessages,
            };
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            Load(rootVisualElement);
        }

        public void Load(VisualElement root)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GetAssetPath("WelcomeWindow.uss"));
            root.styleSheets.Add(styleSheet);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GetAssetPath("WelcomeWindow.uxml"));
            root.Add(visualTree.Instantiate());

            root.Q<Button>("QuickStartButton").clicked += OpenQuickstart;
            root.Q<Button>("SampleScenesButton").clicked += ImportSampleScenes;
            root.Q<Button>("VideoTutorialsButton").clicked += OpenVideos;
            root.Q<Button>("DocsLink").clicked += OpenDocs;
            root.Q<Button>("ForumsLink").clicked += OpenSupport;
            root.Q<Button>("DiscordLink").clicked += OpenDiscord;
            root.Q<Button>("ChangelogLink").clicked += OpenChangelog;

            if (update.IsActive)
            {
                root.Q<Label>("UpdateTitle").text = $"coherence was updated from {update.OldVersion} to {update.Version}";
                root.Q<VisualElement>("UpdateMessage").AddToClassList("show-expand-height");
                titleContent = GUIContents.UpdateTitle;
            }
            else
            {
                root.Q<VisualElement>("UpdateMessage").AddToClassList("hide");
                titleContent = GUIContents.Title;
            }
        }

        private static string GetAssetPath(string assetName) => $"{Paths.welcomeWindowPath}/{assetName}";

        private void OpenQuickstart()
        {
            ReportButtonClickedAnalytic("quickstart");
            _ = CoherenceHub.Open<QuickStartModule>();
        }

        private void ImportSampleScenes()
        {
            ReportButtonClickedAnalytic("samples");
            if (EditorUtility.DisplayDialog("Import Samples", $"Do you want to import '{_sampleScenesAsset.name}' in your project?", "Import Samples", "Cancel"))
            {
                UIUtils.ImportAndPingFromPackageSample(_sampleScenesAsset, null);
            }
        }

        private void OpenVideos()
        {
            ReportButtonClickedAnalytic("videos");
            Application.OpenURL(ExternalLinks.YouTubeChannel);
        }

        private void OpenDocs()
        {
            ReportButtonClickedAnalytic("link_docs");
            Application.OpenURL(DocumentationLinks.GetDocsUrl(DocumentationKeys.Overview));
        }

        private void OpenSupport()
        {
            ReportButtonClickedAnalytic("link_support");
            Application.OpenURL(ExternalLinks.Support);
        }

        private void OpenDiscord()
        {
            ReportButtonClickedAnalytic("link_discord");
            Application.OpenURL(ExternalLinks.Discord);
        }

        private void OpenChangelog()
        {
            ReportButtonClickedAnalytic("link_changelog");
            Application.OpenURL(DocumentationLinks.GetDocsUrl(DocumentationKeys.ReleaseNotes));
        }

        private void ReportButtonClickedAnalytic(string buttonName)
        {
            Analytics.Capture(new Analytics.Event<WelcomeEventProperties>(
                Analytics.Events.WelcomeScreenButtonClicked,
                new WelcomeEventProperties
                {
                    comes_from_update = update.IsActive,
                    button_name = buttonName,
                }
            ));
        }
    }
}
