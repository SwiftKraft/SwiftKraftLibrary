// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using System;
    using UnityEditor;
    using UnityEngine;

    internal class PortalLoginDrawer
    {
        private static readonly int MaxPickListRows = 10;
        public static readonly GUIContent orgLabel = EditorGUIUtility.TrTextContent("Organization");
        public static readonly GUIContent projLabel = EditorGUIUtility.TrTextContent("Project");
        public static readonly GUIContent usage = EditorGUIUtility.TrTextContent("Usage");
        public static readonly GUIContent refresh = Icons.GetContent("Coherence.Sync", "Refresh");
        public static readonly string NoneProjectName = "None";

        /// <summary>
        /// The pending request to get <see cref="OrgSubscription"/> if any.
        /// </summary>
        public static PortalRequest GetSubscriptionDataRequest;

        public static OrganizationSubscription OrgSubscription;

        public static float DrawOrganizationOptions()
        {
            using var scope = new EditorGUILayout.HorizontalScope();

            var organizations = PortalLogin.organizations;
            EditorGUI.BeginChangeCheck();
            using var disabled = new EditorGUI.DisabledScope(string.IsNullOrEmpty(ProjectSettings.instance.LoginToken));
            var labelWidth = Mathf.Max(CoherenceHubLayout.Styles.Label.CalcSizeCeil(orgLabel).x, CoherenceHubLayout.Styles.Label.CalcSizeCeil(projLabel).x);
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var refreshBtnWidth = CoherenceHubLayout.Styles.Button.CalcSize(refresh).x;

            CoherenceHubLayout.DrawLabel(orgLabel, GUILayout.Width(labelWidth), GUILayout.Height(height));

            var controlRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f, CoherenceHubLayout.Styles.PopupNonFixedHeight, GUILayout.MinWidth(100f), GUILayout.MaxWidth(2040f));
            if (EditorGUI.DropdownButton(controlRect, new GUIContent(GetSelectedOrganization()?.name ?? "None"), FocusType.Passive, CoherenceHubLayout.Styles.PopupNonFixedHeight))
            {
                var projectsPopup =
                    new PopupPicker<Organization>(organizations, Math.Min(organizations.Length + 1, MaxPickListRows),
                        controlRect.width, item => item.name);
                projectsPopup.ItemSelected += org =>
                {
                    var rt = ProjectSettings.instance.RuntimeSettings;
                    Undo.RecordObject(rt, "Set Organization");
                    PortalLogin.AssociateOrganization(org);
                    RefreshSubscriptionInfo();
                };
                PopupWindow.Show(controlRect, projectsPopup);
                GUIUtility.ExitGUI();
            }

            DrawOrganizationRefreshButton(refresh, refreshBtnWidth);

            return labelWidth;
        }

        public static void DrawOrganizationRefreshButton(GUIContent content, float width)
        {
            if (CoherenceHubLayout.DrawButton(content, true, null, GUILayout.Width(width)))
            {
                PortalLogin.FetchOrgs();
                RefreshSubscriptionInfo();
            }
        }

        public static void RefreshSubscriptionInfo()
        {
            GetSubscriptionDataRequest = OrganizationSubscription.FetchAsync(ProjectSettings.instance.RuntimeSettings.OrganizationID,
                subscription =>
                {
                    OrgSubscription = subscription;
                    GetSubscriptionDataRequest = null;
                });
        }

        public static Organization GetSelectedOrganization()
        {
            foreach (var org in PortalLogin.organizations)
            {
                if (org.id == ProjectSettings.instance.RuntimeSettings.OrganizationID)
                {
                    return org;
                }
            }

            return null;
        }

        public static string GetSelectedProjectName()
        {
            var selectedOrg = GetSelectedOrganization();
            if (selectedOrg == null)
            {
                return NoneProjectName;
            }

            foreach (var proj in selectedOrg.projects)
            {
                if (proj.id == ProjectSettings.instance.RuntimeSettings.ProjectID)
                {
                    return proj.name;
                }
            }

            return NoneProjectName;
        }

        private static int GetSelectedOrganizationContent(Organization[] organizations)
        {
            for (int i = 0; i < organizations.Length; i++)
            {
                var org = organizations[i];
                if (org.id == ProjectSettings.instance.RuntimeSettings.OrganizationID)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        public static void DrawProjectOptions(float labelWidth)
        {
            var organizations = PortalLogin.organizations;
            var orgContent = GetSelectedOrganizationContent(organizations);

            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                using var disabled = new EditorGUI.DisabledScope(string.IsNullOrEmpty(ProjectSettings.instance.LoginToken));

                CoherenceHubLayout.DrawLabel(new GUIContent(projLabel), GUILayout.Width(labelWidth),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight + 2f));
                var projects = orgContent == 0 ? Array.Empty<ProjectInfo>() : organizations[orgContent - 1].projects;

                var controlRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f, CoherenceHubLayout.Styles.PopupNonFixedHeight, GUILayout.MinWidth(100f), GUILayout.MaxWidth(2040f));

                if (EditorGUI.DropdownButton(controlRect, new GUIContent(GetSelectedProjectName()), FocusType.Passive, CoherenceHubLayout.Styles.PopupNonFixedHeight))
                {
                    var projectsPopup =
                        new PopupPicker<ProjectInfo>(projects, Math.Min(projects.Length + 1, MaxPickListRows),
                            controlRect.width, item => item.name);
                    projectsPopup.ItemSelected += project =>
                    {
                        var rt = ProjectSettings.instance.RuntimeSettings;
                        Undo.RecordObject(rt, "Set Project");
                        PortalLogin.AssociateProject(project, Schemas.UpdateSyncState);
                    };
                    PopupWindow.Show(controlRect, projectsPopup);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}


