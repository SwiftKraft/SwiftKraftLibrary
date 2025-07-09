// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    //Our implementation of UnityEditor.EditorGUILayout
    internal static class CoherenceHubLayout
    {
        private const string skinName = "CoherenceHubSkin";
        private static GUISkin coherenceSkin;
        internal static float SectionHeaderTopSpacing = 8;
        internal static float SectionHeaderBottomSpacing = 6;
        internal static float SectionHorizontalSpacing = 4;
        internal static float SectionSpacing = 8;
        internal static float SectionBottomSpacing = 8;
        internal static float LabelBottomSpacing = 6;

        private static bool showLoginDetails;
        private static GenericPopup loginDetailsPopup;

        private static GUISkin CoherenceSkin
        {
            get
            {
                if (coherenceSkin == null)
                {
                    coherenceSkin = GetCurrentSkin();
                }

                return coherenceSkin;
            }
        }

        private static GUISkin GetCurrentSkin()
        {
            return AssetDatabase.LoadAssetAtPath<GUISkin>(GetSkinPath(EditorGUIUtility.isProSkin));
        }

        private static string GetSkinPath(bool isDark, string overrideName = null)
        {
            return $"{Paths.uiAssetsPath}/{((overrideName != null) ? overrideName : skinName)}{(isDark ? "_dark" : "_light")}.guiskin";
        }

        public static class GUIContents
        {
            public static readonly GUIContent discordLogo = new(Icons.GetContent("Quickstart.Discord.Logo", "Opens a link to our Discord."));
            public static readonly GUIContent documentationIcon = new(Icons.GetContent("Quickstart.Documentation.Icon", "Online documentation."));
            public static readonly GUIContent devPortal = new(Icons.GetContent("Quickstart.DevPortal.Icon", "Open the developer portal."));

            public static readonly GUIContent discordText = new("Join our Discord", "Opens a link to our Discord.");
            public static readonly GUIContent documentationText = new("Documentation", "Online documentation.");
            public static readonly GUIContent devPortalText = new("Developer Portal", "Open the developer portal.");
            public static readonly GUIContent correctlySetupHint = Icons.GetContentWithText("Coherence.IssueWizard.Passed", "", "Everything has been correctly setup");
            public static readonly GUIContent refresh = Icons.GetContent("Coherence.Sync", "Refresh");
            public static readonly GUIContent warning = EditorGUIUtility.TrIconContent("Warning");
        }

        public static class Styles
        {
            public static readonly GUIStyle HeaderToolbarButton = CoherenceSkin.GetStyle("HeaderToolbarButton");
            public static readonly GUIStyle ToolbarButton = CoherenceSkin.GetStyle("ToolbarButton");
            public static readonly GUIStyle ToolbarToggle = CoherenceSkin.GetStyle("toolbarbutton");
            public static readonly GUIStyle BlueButton = CoherenceSkin.GetStyle("BlueButton");
            public static readonly GUIStyle SmallBlueButton = CoherenceSkin.GetStyle("SmallBlueButton");
            public static readonly GUIStyle WarningDismissButton = CoherenceSkin.GetStyle("WarningDismissButton");
            public static readonly GUIStyle Button = new(GUI.skin.button)
            {
                wordWrap = true,
            };
            public static readonly GUIStyle ButtonNoLineWrap = new(GUI.skin.button)
            {
                wordWrap = false,
            };

            public static readonly GUIStyle LabelButtonWithPadding = new(EditorStyles.label)
            {
                padding =
                {
                    left = 8,
                    right = 8,
                },
            };

            public static readonly GUIStyle SmallLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            public static readonly GUIStyle InfoLabel = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            public static readonly GUIStyle Label = new(EditorStyles.label)
            {
                wordWrap = true,
                clipping = TextClipping.Overflow,
            };

            public static readonly GUIStyle BoldLabel = new(Label)
            {
                fontStyle = FontStyle.Bold,
            };

            public static readonly GUIStyle SectionHeader = new(EditorStyles.label)
            {
                alignment = TextAnchor.LowerLeft,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                stretchHeight = true,
                richText = true,
                clipping = TextClipping.Overflow
            };

            public static readonly GUIStyle VersionHeader = new(EditorStyles.linkLabel)
            {
                fontStyle = FontStyle.Normal,
                normal =
                {
                    textColor = GetCoherencePrimaryColor(),
                },
            };

            public static readonly GUIStyle Grid = new(EditorStyles.miniButton)
            {
                stretchWidth = true,
            };

            public static readonly GUIStyle Bullet = new(Label)
            {
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
            };

            public static readonly GUIStyle HeaderBackgroundWithLogin = new()
            {
                padding =
                {
                    bottom = 4,
                    top = 4,
                    left = 8,
                    right = 20,
                },
                fixedHeight = 48f,
            };

            public static readonly GUIStyle HeaderBackground = new GUIStyle()
            {
                padding =
                {
                    bottom = 4,
                    top = 4,
                    left = 4,
                    right = 24,
                },
                fixedHeight = 48f,
            };

            public static readonly GUIStyle HeaderToolbar = new()
            {
                padding =
                {
                    bottom = 4,
                    top = 4,
                    left = 8,
                    right = 20,
                },
            };

            public static readonly GUIStyle SectionBox = new(EditorStyles.helpBox);

            public static readonly GUIStyle PopupNonFixedHeight = new(EditorStyles.popup)
            {
                fixedHeight = 0f,
            };

            public static readonly GUIStyle HorizontalMargins = new()
            {
                margin =
                {
                    bottom = 0,
                    top = 0,
                    left = 8,
                    right = 8,
                },
            };
        }

        internal static bool DrawHelpFoldout(bool showing, HubModule.HelpSection help)
        {
            if (!ProjectSettings.instance.showHubModuleQuickHelp)
            {
                return false;
            }

            bool bExpand;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bExpand = CustomFoldout(showing, help.title, Styles.InfoLabel);

                if (bExpand)
                {
                    EditorGUILayout.Space(SectionHorizontalSpacing);
                    DrawListLabel(help.content);
                    EditorGUILayout.Space(SectionHorizontalSpacing);
                }
            }
            return bExpand;
        }

        internal static bool CustomFoldout(bool value, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            content.image = value ? EditorGUIUtility.IconContent("IN_foldout_on").image : EditorGUIUtility.IconContent("IN_foldout").image;

            if (GUILayout.Button(content, style, options))
            {
                return !value;
            }

            return value;
        }

        public static Color GetCoherencePrimaryColor()
        {
            ColorUtility.TryParseHtmlString("#29ABE2", out var color);
            return color;
        }

        internal static void DrawBulletPoint(GUIContent content, int indent = 1)
        {
            for (int i = 0; i < indent; i++)
            {
                EditorGUI.indentLevel++;
            }

            var bulletContent = new GUIContent(content);
            bulletContent.text = "\u2022 " + bulletContent.text;
            var contentWidth = Styles.Bullet.CalcSize(bulletContent).x;
            EditorGUIUtility.labelWidth = contentWidth;
            EditorGUILayout.LabelField(bulletContent, Styles.Bullet);
            EditorGUIUtility.labelWidth = 0;

            for (int i = 0; i < indent; i++)
            {
                EditorGUI.indentLevel--;
            }
        }

        public static void DrawDiskPath(string currentPath, string infoString, Func<string> unityPanel, Action<string> onChangePath)
        {
            _ = EditorGUILayout.BeginHorizontal(GUI.skin.box);
            var text = string.IsNullOrEmpty(currentPath) ? infoString : currentPath;
            EditorGUILayout.SelectableLabel(text, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                string path = unityPanel.Invoke();

                if (!string.IsNullOrEmpty(path))
                {
                    onChangePath?.Invoke(path);
                }

                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        public static bool DrawToggle(string text, bool value, params GUILayoutOption[] options)
        {
            return EditorGUILayout.Toggle(text, value, options);
        }

        public static string DrawTextField(string text, params GUILayoutOption[] options)
        {
            return EditorGUILayout.TextField(text, options);
        }

        public static string DrawTextField(string label, string text, params GUILayoutOption[] options)
        {
            return EditorGUILayout.TextField(label, text, options);
        }

        public static int DrawIntField(string label, int value, params GUILayoutOption[] options)
        {
            return EditorGUILayout.IntField(label, value, options);
        }

        public static int DrawGrid(int tabindex, GUIContent[] contents, params GUILayoutOption[] options)
        {
            var style = Styles.Grid;
            var widestContent = GUIContent.none;
            foreach (var content in contents)
            {
                if (content.text.Length > widestContent.text.Length)
                {
                    widestContent = content;
                }
            }

            var controlWidth = style.CalcSize(widestContent).x;
            var width = EditorGUIUtility.currentViewWidth;
            var columnCount = Mathf.FloorToInt(width / controlWidth);
            columnCount = Mathf.Clamp(columnCount, 1, contents.Length);
            return GUILayout.SelectionGrid(tabindex, contents, columnCount, style, options);
        }

        public static void DrawBoldLabel(GUIContent content, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(content, Styles.BoldLabel, options);
        }

        public static void DrawListLabel(GUIContent content, params GUILayoutOption[] options)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(content, Styles.Label, options);
            }
        }

        public static void DrawLabel(GUIContent content, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(content, Styles.Label, options);
        }

        internal static void DrawLabel(GUIContent label, GUIContent content, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(label, content, Styles.Label, options);
        }


        public static void DrawMessageArea(GUIContent content)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.infoicon.sml"));
                EditorGUILayout.LabelField(content, Styles.InfoLabel);
            }
        }

        public static void DrawMessageArea(string text)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.infoicon.sml"));
                EditorGUILayout.LabelField(text, Styles.InfoLabel);
            }
        }

        public static void DrawWarnArea(GUIContent content, bool useBox = true)
        {
            if (useBox)
            {
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    DrawIconButton(EditorGUIUtility.IconContent("console.warnicon.sml"));
                    EditorGUILayout.LabelField(content, Styles.InfoLabel);
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawIconButton(EditorGUIUtility.IconContent("console.warnicon.sml"));
                    EditorGUILayout.LabelField(content, Styles.InfoLabel);
                }
            }
        }

        public static bool DrawWarnArea(string text, bool useBox = true, bool dismissible = false)
        {
            var skin = useBox ? GUI.skin.box : GUIStyle.none;

            using (new EditorGUILayout.HorizontalScope(skin))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.warnicon.sml"));
                EditorGUILayout.LabelField(text, Styles.InfoLabel);

                if (dismissible)
                {

                    return GUILayout.Button(EditorGUIUtility.TrIconContent("d_winbtn_win_close"), Styles.WarningDismissButton);
                }
            }

            return false;
        }

        public static void DrawErrorArea(string text)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.erroricon.sml")) ;
                EditorGUILayout.LabelField(text, Styles.InfoLabel);
            }
        }

        public static void DrawInfoAreaWithBulletPoints(string text, params string[] bulletPoints)
        {
            if (!ProjectSettings.instance.showHubModuleQuickHelp)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawIconButton(EditorGUIUtility.IconContent("console.infoicon.sml"));
                    DrawLabel(new GUIContent(text));
                }

                foreach (var bulletPoint in bulletPoints)
                {
                    DrawBulletPoint(new GUIContent(bulletPoint), 2);
                }
            }
        }

        public static void DrawInfoLabel(GUIContent content)
        {
            if (!ProjectSettings.instance.showHubModuleQuickHelp)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(content, Styles.InfoLabel);
                GUILayout.Space(LabelBottomSpacing);
            }
        }

        public static void DrawInfoLabel(string text)
        {
            if (!ProjectSettings.instance.showHubModuleQuickHelp)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(text, Styles.InfoLabel);
                GUILayout.Space(LabelBottomSpacing);
            }
        }

        public static bool DrawIconButton(GUIContent content)
        {
            return GUILayout.Button(content, EditorStyles.label, GUILayout.ExpandWidth(false));
        }

        public static bool DrawButton(GUIContent content, bool allowWordWrap = true, GUIStyle buttonStyle = null, params GUILayoutOption[] options)
        {
            var style = buttonStyle ?? (allowWordWrap ? Styles.Button : Styles.ButtonNoLineWrap);
            return GUILayout.Button(content, style, options);
        }

        public static bool DrawButton(string text, bool allowWordWrap = true, GUIStyle buttonStyle = null, params GUILayoutOption[] options)
        {
            var style = buttonStyle ?? (allowWordWrap ? Styles.Button : Styles.ButtonNoLineWrap);
            return GUILayout.Button(text, style, options);
        }

        internal static bool DrawButtonBlue(GUIContent content, params GUILayoutOption[] options)
        {
            return GUILayout.Button(content, Styles.BlueButton, options);
        }

        internal static bool DrawButtonBlue(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, Styles.BlueButton, options);
        }

        internal static void DrawCloudDependantButton(GUIContent content, Action onButtonPress, string tooltip, Func<bool> disableConditions = null, GUIStyle buttonStyle = null, params GUILayoutOption[] options)
        {
            var additionalDisableConditions = disableConditions?.Invoke() ?? false;

            var isPortalAvailable = PortalUtil.CanCommunicateWithPortal;
            GUIContent conditionalContent = null;

            if (!isPortalAvailable || !string.IsNullOrEmpty(tooltip))
            {
                conditionalContent = new GUIContent(content);
                if (conditionalContent.image == null)
                {
                    conditionalContent.image = isPortalAvailable
                        ? null
                        : GUIContents.warning.image;
                }

                conditionalContent.tooltip = tooltip;
            }

            using (new EditorGUI.DisabledScope(!PortalUtil.CanCommunicateWithPortal || additionalDisableConditions))
            {
                if (DrawButton(conditionalContent ?? content, true, buttonStyle, options))
                {
                    onButtonPress.Invoke();
                    GUIUtility.ExitGUI();
                }
            }
        }

        public static bool DrawButtonWithPadding(GUIContent content, params GUILayoutOption[] options)
        {
            return GUILayout.Button(content, Styles.LabelButtonWithPadding, options);
        }

        internal static void DrawPrefixLabel(GUIContent content, GUIStyle followingStyle = null)
        {
            EditorGUILayout.PrefixLabel(content, followingStyle ?? EditorStyles.miniButton, EditorStyles.label);
        }

        public static void DrawActionLabel(GUIContent content, Action action)
        {
            if (EditorGUILayout.LinkButton(content))
            {
                action?.Invoke();
            }
        }

        public static void DrawLink(GUIContent content, string url)
        {
            DrawActionLabel(content, () => Application.OpenURL(url));
        }

        public static void DrawSection(string title, Action sectionContent, bool bottomSpacing = true)
        {
            using (var vScope = new EditorGUILayout.VerticalScope(Styles.SectionBox))
            {
                DrawSectionHeader(title);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(SectionHorizontalSpacing);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        sectionContent?.Invoke();
                    }
                    GUILayout.Space(SectionHorizontalSpacing);
                }
                GUILayout.Space(SectionBottomSpacing);
            }

            if (bottomSpacing)
            {
                GUILayout.Space(SectionSpacing);
            }
        }

        public static void DrawSection(GUIContent content, Action sectionContent, Action UICallback = null, float? customSectionSpacing = null)
        {
            using (new EditorGUILayout.VerticalScope(Styles.SectionBox))
            {
                DrawSectionHeader(content, UICallback);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(SectionHorizontalSpacing);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        sectionContent?.Invoke();
                    }
                    GUILayout.Space(SectionHorizontalSpacing);
                }
                GUILayout.Space(SectionBottomSpacing);
            }

            var sectionSpacing = customSectionSpacing.HasValue ? customSectionSpacing.Value : SectionSpacing;
            GUILayout.Space(sectionSpacing);
        }

        public static void DrawSectionHeader(string text, Action UICallback = null)
        {
            GUILayout.Space(SectionHeaderTopSpacing);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(SectionHorizontalSpacing);
                EditorGUILayout.LabelField(text, Styles.SectionHeader);
                GUILayout.FlexibleSpace();
                UICallback?.Invoke();
                GUILayout.Space(SectionHorizontalSpacing);
            }
            GUILayout.Space(SectionHeaderBottomSpacing);
        }

        private static void DrawSectionHeader(GUIContent content, Action UICallback = null)
        {
            GUILayout.Space(SectionHeaderTopSpacing);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(SectionHorizontalSpacing);

                var titleWidth = Styles.SectionHeader.CalcSize(content).x;

                EditorGUILayout.LabelField(content, Styles.SectionHeader, GUILayout.Width(titleWidth));

                GUILayout.FlexibleSpace();
                UICallback?.Invoke();
                GUILayout.Space(SectionHorizontalSpacing);
            }
            GUILayout.Space(SectionHeaderBottomSpacing);
        }
    }
}
