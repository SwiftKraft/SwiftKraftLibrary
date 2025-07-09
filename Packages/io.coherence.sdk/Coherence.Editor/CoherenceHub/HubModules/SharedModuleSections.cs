// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using Coherence.Editor.Portal;
using Coherence.Editor.Toolkit;
using UnityEditor;
using UnityEngine;

namespace Coherence.Editor
{
    internal static class SharedModuleSections
    {
        private static class GUIContents
        {
            public static readonly GUIContent syncStatus = EditorGUIUtility.TrTextContent("Schema in Cloud");
            public static readonly GUIContent localSchemaId = new("Local Schema ID");
        }

        internal static void DrawSchemasInPortal()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var text = BakeUtil.HasSchemaID ? BakeUtil.SchemaID.Substring(0, 5) : "No Schema";
                var content = new GUIContent(text);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(GUIContents.localSchemaId);
                    GUILayout.Label(content, GUILayout.ExpandWidth(false));
                    if (GUILayout.Button(ContentUtils.GUIContents.clipboard, ContentUtils.GUIStyles.iconButton))
                    {
                        EditorGUIUtility.systemCopyBuffer = BakeUtil.SchemaID;
                        EditorWindow.focusedWindow.ShowNotification(new GUIContent("Schema ID copied to clipboard"));
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var content = PortalUtil.OrgAndProjectIsSet ? Schemas.StateContent : new GUIContent();
                CoherenceHubLayout.DrawLabel(GUIContents.syncStatus, content, options:GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();

                CoherenceHubLayout.DrawCloudDependantButton(CoherenceHubLayout.GUIContents.refresh, Schemas.UpdateSyncState, string.Empty);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawCloudDependantButton(CloudModule.ModuleGUIContents.Upload, () =>
                {
                    Schemas.UploadActive(InteractionMode.UserAction);
                }, "You need to login to sync schemas.",
                () => !PortalUtil.OrgAndProjectIsSet,
                ContentUtils.GUIStyles.bigButton);
            }

            if (PortalUtil.OrgAndProjectIsSet)
            {
                GUILayout.Label("Worlds must be edited from the Dashboard to use a different schema.", ContentUtils.GUIStyles.miniLabelGreyWrap);
            }
        }

        internal static string GetDashboardUrl(string organizationSlug)
        {
            var org = organizationSlug ?? string.Empty;
            var url = $"{ExternalLinks.PortalUrl}/{PortalUrlMangler(org)}";
            return url;
        }

        internal static string GetOrganizationUsageUrl(string organizationSlug)
            => $"{GetDashboardUrl(organizationSlug)}/usage";

        internal static string GetOrganizationBillingUrl(string organizationSlug)
            => $"{GetDashboardUrl(organizationSlug)}/billing";

        internal static string GetDashboardWorldsUrl(string projectName, string organizationSlug)
        {
            var proj = projectName ?? string.Empty;
            var org = organizationSlug ?? string.Empty;
            string url = projectName == PortalLoginDrawer.NoneProjectName ?
                Endpoints.portalUrl :
                $"{ExternalLinks.PortalUrl}/{PortalUrlMangler(org)}/{PortalUrlMangler(proj)}/worlds";

            return url;
        }

        internal static string GetDashboardProjectUrl(string projectName, string organizationName)
        {
            var proj = projectName ?? string.Empty;
            var org = organizationName ?? string.Empty;
            string url = projectName == PortalLoginDrawer.NoneProjectName ?
                Endpoints.portalUrl :
                $"{ExternalLinks.PortalUrl}/{PortalUrlMangler(org)}/{PortalUrlMangler(proj)}";

            return url;
        }

        private static string PortalUrlMangler(string url)
        {
            return url.ToLower().Replace(" ", "-");
        }
    }
}
