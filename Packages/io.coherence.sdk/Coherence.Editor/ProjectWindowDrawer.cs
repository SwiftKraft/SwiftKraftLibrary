namespace Coherence.Editor
{
    using Portal;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class ProjectWindowDrawer
    {
        private static class GUIContents
        {
            public static readonly GUIContent Clone = EditorGUIUtility.TrIconContent(
            Icons.GetPath("Coherence.Clone"),
            "This Editor instance is a Clone. Clones don't trigger asset automations such as auto-updating prefabs, and features such as baking are disabled.");

            public static readonly GUIContent BakeOutdated = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Coherence.Bake.Warning"),
                "Bake required for networking.\n\nClick to bake.");

            public static readonly GUIContent NotLoggedIn = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Logo.Icon.Disabled"),
                "Bake up-to-date.\nNot logged in to coherence Cloud.");

            public static readonly GUIContent CloudOutOfSync = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Coherence.Cloud.Warning"),
                "Schema not found in Cloud.\n\nClick to upload.");

            public static readonly GUIContent NoOrganizationSelected = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Coherence.Cloud.Warning"),
                "No organization selected.\n\nClick to open coherence Cloud window.");

            public static readonly GUIContent NoProjectSelected = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Coherence.Cloud.Warning"),
                "No project selected.\n\nClick to open coherence Cloud window.");

            public static readonly GUIContent StatusLoggedIn = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Logo.Icon"),
                "Bake up-to-date.\nLogged in to coherence Cloud.");
        }

        private static string coherenceFolderGuid;

        static ProjectWindowDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += OnItemGUI;
            EditorApplication.projectChanged += OnProjectChanged;
            UpdateFolderGuid();
        }

        private static void OnProjectChanged() => UpdateFolderGuid();
        private static void UpdateFolderGuid() => coherenceFolderGuid = AssetDatabase.AssetPathToGUID(Paths.projectAssetsPath);

        private static bool HasOrganization => !string.IsNullOrEmpty(RuntimeSettings.Instance.OrganizationID);
        private static bool HasProject => !string.IsNullOrEmpty(RuntimeSettings.Instance.ProjectID);

        private static void OnItemGUI(string guid, Rect rect)
        {
            if (guid != coherenceFolderGuid)
            {
                return;
            }

            // only render at smallest height
            var smallestHeight = 16f;
            if (!Mathf.Approximately(rect.height, smallestHeight))
            {
                return;
            }

            // precalculated size needed to render a folder with the name "coherence"
            var usedWidth = 80f;
            var iconWidth = 16f;
            if (rect.width <= usedWidth + iconWidth)
            {
                return;
            }

            var iconRect = rect;
            iconRect.xMin = iconRect.xMax - iconWidth;

            if (CloneMode.Enabled)
            {
                DrawIconButton(iconRect, GUIContents.Clone, true);
                return;
            }

            if (BakeUtil.Outdated)
            {
                if (DrawIconButton(iconRect,  GUIContents.BakeOutdated))
                {
                    BakeUtil.Bake();
                }
                return;
            }

            if (!PortalLogin.IsLoggedIn)
            {
                if (DrawIconButton(iconRect, GUIContents.NotLoggedIn))
                {
                    CoherenceHub.Open<CloudModule>();
                }
                return;
            }

            if (!HasOrganization)
            {
                if (DrawIconButton(iconRect, GUIContents.NoOrganizationSelected))
                {
                    CoherenceHub.Open<CloudModule>();
                }
                return;
            }

            if (!HasProject)
            {
                if (DrawIconButton(iconRect, GUIContents.NoProjectSelected))
                {
                    CoherenceHub.Open<CloudModule>();
                }
                return;
            }

            var org = RuntimeSettings.Instance.OrganizationName;
            var project = RuntimeSettings.Instance.ProjectName;
            var id = string.IsNullOrEmpty(org) ? project : $"{org}/{project}";

            if (PortalUtil.SyncState != Schemas.SyncState.InSync)
            {
                GUIContents.CloudOutOfSync.tooltip = $"Schema not found in Cloud.\nProject '{id}'\n\nClick to upload.";
                if (DrawIconButton(iconRect, GUIContents.CloudOutOfSync))
                {
                    Schemas.UploadActive(InteractionMode.UserAction);
                }
                return;
            }

            GUIContents.StatusLoggedIn.tooltip = $"Logged in to coherence Cloud.\nProject '{id}'";
            if (DrawIconButton(iconRect, GUIContents.StatusLoggedIn))
            {
                CoherenceHub.Open();
            }
        }

        private static bool DrawIconButton(Rect rect, GUIContent content, bool disabled = false)
        {
            EditorGUI.BeginDisabledGroup(disabled);

            var style = disabled ? GUIStyle.none : ContentUtils.GUIStyles.iconButton;
            var clicked = GUI.Button(rect, content, style);

            EditorGUI.EndDisabledGroup();

            return clicked;
        }
    }
}
