// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using Portal;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal class CoherenceHeader
    {
        private static class GUIContents
        {
            public static readonly GUIContent logo = Icons.GetContent("Logo.Optimized");
            public static readonly GUIContent menu = EditorGUIUtility.TrIconContent("_Menu");
            public static readonly GUIContent tempContent = new();
        }

        private static class GUIStyles
        {
            public static readonly GUIStyle slimHeader = new()
            {
                padding = new RectOffset(EditorStyles.inspectorDefaultMargins.padding.left - 2,
                    EditorStyles.inspectorDefaultMargins.padding.right, 2, 2),
            };
        }

        private EditorWindow parent;
        private Texture headerTexture;
        private Vector2 logoSize = new(130f, 30f);
        private Vector2 inspectorLogoSize = new(130f, 20f);
        private bool showLoginDetails;
        private GenericPopup loginDetailsPopup;
        private bool breakOutOfOnGUIWithLogin;

        public CoherenceHeader(EditorWindow parent)
        {
            this.parent = parent;
        }

        internal void OnGUI()
        {
            LoadHeaderTexture();

            using (var scope = new EditorGUILayout.HorizontalScope(CoherenceHubLayout.Styles.HeaderBackground))
            {
                DrawLogoAndBackground(scope);

                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();

                    var width = CoherenceHubLayout.Styles.VersionHeader
                        .CalcSize(new GUIContent(CoherenceHub.Info.ToString())).x;
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(width));
                    DrawVersionLabel(rect);

                    GUILayout.FlexibleSpace();
                }
            }
        }

        internal static void OnSlimHeader(string description)
        {
            DrawSlimHeader(description);
        }

        internal void OnGUIWithLogin()
        {
            EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
            LoadHeaderTexture();

            using (var scope = new EditorGUILayout.HorizontalScope(CoherenceHubLayout.Styles.HeaderBackgroundWithLogin))
            {
                DrawLogoAndBackground(scope);

                var rect = GetVersionLabelRect(CoherenceHubLayout.Styles.HeaderBackgroundWithLogin.fixedHeight);

                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();
                    bool loginToken = !string.IsNullOrEmpty(ProjectSettings.instance.LoginToken);

                    if (loginToken)
                    {
                        DrawLoggedInUi(rect);
                    }
                    else
                    {
                        DrawLoggedOutUi(rect);
                    }

                    GUILayout.FlexibleSpace();
                    if (breakOutOfOnGUIWithLogin)
                    {
                        breakOutOfOnGUIWithLogin = false;
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        internal static void DrawSlimHeader(string description, Object target = null, MenuItemData[] menuItems = null)
        {
            var t = GUIContents.tempContent;
            t.image = GUIContents.logo.image;
            t.tooltip = description;
            t.text = null;

            var style = GUIStyles.slimHeader;
            var rect = EditorGUILayout.GetControlRect(false, style.CalcSize(t).y, style);
            EditorGUI.DrawRect(rect, Color.black);

            var buttonWidth = 16f;
            var marginRight = 5f;
            var marginTop = 2f;

            var logoRect = rect;
            logoRect.xMax -= buttonWidth - marginRight;
            GUI.Label(logoRect, t, style);

            if (menuItems != null)
            {
                var menuRect = rect;
                menuRect.xMin = menuRect.xMax - buttonWidth - marginRight;
                menuRect.yMin += marginTop;
                if (GUI.Button(menuRect, GUIContents.menu, ContentUtils.GUIStyles.iconButton))
                {
                    var menu = new GenericMenu();
                    foreach (var menuItem in menuItems)
                    {
                        menu.AddItem(menuItem.content, menuItem.isOn, menuItem.function, target);
                    }

                    menu.ShowAsContext();
                }
            }
        }

        private void DrawLogoAndBackground(EditorGUILayout.HorizontalScope scope)
        {
            EditorGUI.DrawRect(scope.rect, Color.black);

            EditorGUILayout.Separator();

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(logoSize.x)))
            {
                GUILayout.FlexibleSpace();
                var nextRect = EditorGUILayout.GetControlRect(false, logoSize.y,
                    CoherenceHubLayout.Styles.HeaderBackgroundWithLogin);
                GUI.DrawTexture(nextRect, headerTexture);
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawLogoAndBackgroundForInspector(EditorGUILayout.HorizontalScope scope)
        {
            EditorGUI.DrawRect(scope.rect, Color.black);

            using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorDefaultMargins,
                       GUILayout.Width(inspectorLogoSize.x)))
            {
                GUILayout.FlexibleSpace();
                var nextRect = EditorGUILayout.GetControlRect(false, inspectorLogoSize.y,
                    CoherenceHubLayout.Styles.HeaderBackgroundWithLogin);
                GUI.DrawTexture(nextRect, headerTexture);
                GUILayout.FlexibleSpace();
            }
        }

        private void LoadHeaderTexture()
        {
            if (headerTexture == null)
            {
                headerTexture = AssetDatabase.LoadAssetAtPath<Texture>(Icons.GetPath("d_Coherence.Logo@2x"));
            }
        }

        private Rect GetVersionLabelRect(float rectHeight)
        {
            var maxSize = CoherenceHubLayout.Styles.HeaderBackgroundWithLogin.padding.left + logoSize.x;

            var width = CoherenceHubLayout.Styles.VersionHeader.CalcSize(new GUIContent(CoherenceHub.Info.ToString())).x;
            width += CoherenceHubLayout.Styles.VersionHeader.margin.horizontal +
                     CoherenceHubLayout.Styles.VersionHeader.padding.horizontal;

            var versionLabelRect = new Rect
            {
                x = maxSize + 5f,
                y = (rectHeight / 2f) - 10f,
                width = width,
                height = EditorGUIUtility.singleLineHeight
            };

            return versionLabelRect;
        }

        private void DrawLoggedOutUi(Rect versionLabelRect)
        {
            var guiContent = new GUIContent("Signup / Login");
            var loginSize = new Vector2(100, 22);
            using (new EditorGUILayout.HorizontalScope())
            {
                var nextRect = EditorGUILayout.GetControlRect(false, loginSize.y, CoherenceHubLayout.Styles.BlueButton,
                    GUILayout.Width(loginSize.x + 5f));

                TryToDrawVersionLabel(versionLabelRect, nextRect);

                if (GUI.Button(nextRect, guiContent, CoherenceHubLayout.Styles.BlueButton))
                {
                    breakOutOfOnGUIWithLogin = true;
                    PortalLogin.Login(() =>
                    {
                        CoherenceHub.FocusModule<CloudModule>();
                        GUIUtility.ExitGUI();
                    });
                }
            }
        }

        private void TryToDrawVersionLabel(Rect versionLabelRect, Rect nextRect)
        {
            if (versionLabelRect.xMax <= nextRect.xMin)
            {
                DrawVersionLabel(versionLabelRect);
            }
        }

        private void DrawLoggedInUi(Rect versionLabelRect)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var username = GetEmailUsername();

                var labelContent = new GUIContent(username);
                var buttonContent = EditorGUIUtility.IconContent("d_dropdown@2x");
                var buttonWidth = EditorStyles.label.CalcSize(buttonContent).x;
                var maxLabelWidth = GetMaxLabelWidth(buttonWidth);
                var labelStyle = EditorStyles.whiteLabel;

                var rect = labelStyle.DrawLabel(labelContent, maxLabelWidth);

                TryToDrawVersionLabel(versionLabelRect, rect);

                var buttonStyle = ContentUtils.GUIStyles.iconButton;
                var buttonRect = EditorGUILayout.GetControlRect(false, 16f, buttonStyle, GUILayout.Width(16f));
                buttonRect.y += 2f; // adjust for alignment with label
                if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Account"), false, () => Application.OpenURL("https://coherence.io/dashboard/account"));
                    menu.AddItem(new GUIContent("Logout"), false, PortalLogin.Logout);
                    menu.ShowAsContext();
                }

                ShowLoginDetailsPopupWindow(rect);
            }
        }

        private float GetMaxLabelWidth(float buttonWidth)
        {
            var paddingLeft = CoherenceHubLayout.Styles.HeaderBackgroundWithLogin.padding.left + logoSize.x;
            var availableWidth = parent.position.width - paddingLeft -
                CoherenceHubLayout.Styles.HeaderBackgroundWithLogin.padding.right;
            return availableWidth - buttonWidth - EditorStyles.label.margin.left - EditorStyles.label.margin.right;
        }

        private string GetEmailUsername()
        {
            var email = ProjectSettings.instance.Email;
            var username = email.Remove(email.IndexOf("@", StringComparison.InvariantCulture));
            username = username.Length == 1 ? username.ToUpper() : char.ToUpper(username[0]) + username[1..];
            return username;
        }

        private void DrawVersionLabel(Rect rect)
        {
            if (GUI.Button(rect, new GUIContent(CoherenceHub.Info.ToString(), "Copy coherence data to clipboard."),
                    CoherenceHubLayout.Styles.VersionHeader))
            {
                CoherenceHub.Info.CopyToClipBoard();
            }
        }

        private void ShowLoginDetailsPopupWindow(Rect rect)
        {
            if (Event.current.type != EventType.MouseMove)
            {
                return;
            }

            if (rect.Contains(Event.current.mousePosition) && !showLoginDetails)
            {
                showLoginDetails = true;

                loginDetailsPopup = new GenericPopup(DrawLoginDetails, PopupSize);
                PopupWindow.Show(rect, loginDetailsPopup);
                GUIUtility.ExitGUI();
            }
            else if (!rect.Contains(Event.current.mousePosition) && showLoginDetails)
            {
                loginDetailsPopup?.Close();
                showLoginDetails = false;
                GUIUtility.ExitGUI();
            }
        }

        private Vector2 PopupSize()
        {
            return new Vector2(GetLargestText() + 35f, EditorGUIUtility.singleLineHeight * 3f + 10f);
        }

        private void DrawLoginDetails()
        {
            DrawCenteredText(ProjectSettings.instance.Email);
            DrawCenteredText(GetOrganizationString());
            DrawCenteredText(GetProjectString());
        }

        private string GetProjectString()
        {
            return $"Project: {PortalLoginDrawer.GetSelectedProjectName()}";
        }

        private string GetOrganizationString()
        {
            return $"Org: {PortalLoginDrawer.GetSelectedOrganization()?.name ?? "None"}";
        }

        private float GetLargestText()
        {
            var emailContent = new GUIContent(ProjectSettings.instance.Email);
            var widthEmail = EditorStyles.label.CalcSize(emailContent).x;

            var orgContent = new GUIContent(GetOrganizationString());
            var widthOrg = EditorStyles.label.CalcSize(orgContent).x;

            var projContent = new GUIContent(GetProjectString());
            var widthProj = EditorStyles.label.CalcSize(projContent).x;

            var max = widthEmail;

            if (widthOrg > max)
            {
                max = widthOrg;
            }

            if (widthProj > max)
            {
                max = widthProj;
            }

            return max;
        }

        private void DrawCenteredText(string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var labelContent = new GUIContent(text);
                var labelWidth = EditorStyles.label.CalcSize(labelContent).x;
                EditorGUILayout.LabelField(labelContent, GUILayout.Width(labelWidth));
                GUILayout.FlexibleSpace();
            }
        }
    }
}
