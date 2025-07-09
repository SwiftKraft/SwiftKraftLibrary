// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Interpolation;
    using UnityEditor;
    using UnityEngine;

    internal static class ContentUtils
    {
        private static readonly Stack<Color> colorStack = new();
        private static readonly Stack<Color> backgroundColorStack = new();

        // TODO: Refactor this because it's mildly annoying to maintain
        private static readonly Dictionary<SchemaType, GUIContent> schemaTypeMap = new()
        {
            [SchemaType.Int8] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Bool"), "SByte"),
            [SchemaType.UInt8] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Bool"), "Byte"),
            [SchemaType.Int16] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Int"), "Short"),
            [SchemaType.UInt16] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Int"), "UShort"),
            [SchemaType.Char] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Color"), "Char"),
            [SchemaType.Int] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Int"), "Int"),
            [SchemaType.Bool] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Bool"), "Bool"),
            [SchemaType.Float] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Float"), "Float"),
            [SchemaType.Vector2] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Vector2"), "Vector2"),
            [SchemaType.Vector3] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Vector3"), "Vector3"),
            [SchemaType.String] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.String"), "String"),
            [SchemaType.Quaternion] =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Quaternion"), "Quaternion"),
            [SchemaType.Entity] =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Ref"), "Entity Reference"),
            [SchemaType.Color] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Color"), "Color"),
            [SchemaType.Int64] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Int"), "Int64"),
            [SchemaType.UInt] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Int"), "UInt"),
            [SchemaType.UInt64] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Int"), "UInt64"),
            [SchemaType.Float64] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Float"), "Float64"),
            [SchemaType.Bytes] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Type.Float"), "Bytes"),
            // TODO unknown
        };

        private static readonly Dictionary<SchemaType, GUIContent> schemaTypeButtonMap = new()
        {
            [SchemaType.Int8] = EditorGUIUtility.TrTextContentWithIcon("SByte", Icons.GetPath("Coherence.Type.Bool")),
            [SchemaType.UInt8] = EditorGUIUtility.TrTextContentWithIcon("Byte", Icons.GetPath("Coherence.Type.Bool")),
            [SchemaType.Int16] = EditorGUIUtility.TrTextContentWithIcon("Short", Icons.GetPath("Coherence.Type.Int")),
            [SchemaType.UInt16] = EditorGUIUtility.TrTextContentWithIcon("UShort", Icons.GetPath("Coherence.Type.Int")),
            [SchemaType.Char] = EditorGUIUtility.TrTextContentWithIcon("Char", Icons.GetPath("Coherence.Type.Color")),
            [SchemaType.Int] = EditorGUIUtility.TrTextContentWithIcon("Integer", Icons.GetPath("Coherence.Type.Int")),
            [SchemaType.Bool] = EditorGUIUtility.TrTextContentWithIcon("Boolean", Icons.GetPath("Coherence.Type.Bool")),
            [SchemaType.Float] = EditorGUIUtility.TrTextContentWithIcon("Float", Icons.GetPath("Coherence.Type.Float")),
            [SchemaType.Vector2] =
                EditorGUIUtility.TrTextContentWithIcon("Vector2", Icons.GetPath("Coherence.Type.Vector2")),
            [SchemaType.Vector3] =
                EditorGUIUtility.TrTextContentWithIcon("Vector3", Icons.GetPath("Coherence.Type.Vector3")),
            [SchemaType.String] =
                EditorGUIUtility.TrTextContentWithIcon("String", Icons.GetPath("Coherence.Type.String")),
            [SchemaType.Quaternion] =
                EditorGUIUtility.TrTextContentWithIcon("Quaternion", Icons.GetPath("Coherence.Type.Quaternion")),
            [SchemaType.Entity] =
                EditorGUIUtility.TrTextContentWithIcon("Entity Reference", Icons.GetPath("Coherence.Type.Ref")),
            [SchemaType.Color] = EditorGUIUtility.TrTextContentWithIcon("Color", Icons.GetPath("Coherence.Type.Color")),
            [SchemaType.Int64] = EditorGUIUtility.TrTextContentWithIcon("Int64", Icons.GetPath("Coherence.Type.Int")),
            [SchemaType.UInt] = EditorGUIUtility.TrTextContentWithIcon("UInt", Icons.GetPath("Coherence.Type.Int")),
            [SchemaType.UInt64] = EditorGUIUtility.TrTextContentWithIcon("UInt64", Icons.GetPath("Coherence.Type.Int")),
            [SchemaType.Float64] =
                EditorGUIUtility.TrTextContentWithIcon("Float64", Icons.GetPath("Coherence.Type.Float")),
            [SchemaType.Bytes] = EditorGUIUtility.TrTextContentWithIcon("Bytes", Icons.GetPath("Coherence.Type.Float")),
            // TODO unknown
        };


        public static class GUIContents
        {
            public static readonly GUIContent help = EditorGUIUtility.TrIconContent("_help");

            public static readonly GUIContent command =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Command"), "Command");

            public static readonly GUIContent interpolationNone =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Interpolation.None"), "Interpolation");

            public static readonly GUIContent interpolationLinear =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Interpolation.Linear"), "Interpolation");

            public static readonly GUIContent interpolationCatmullRom =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Interpolation.CatmullRom"), "Interpolation");

            public static readonly GUIContent binding =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Binding"), "Binding");

            public static readonly GUIContent clipboard =
                EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Clipboard"), "Copy to Clipboard");

            public static readonly IReadOnlyDictionary<MessageTarget, GUIContent> routingIcons =
                new Dictionary<MessageTarget, GUIContent>
                {
                    [MessageTarget.AuthorityOnly] =
                        EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.MessageTarget.AuthorityOnly")),
                    [MessageTarget.All] = EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.MessageTarget.All")),
                };

            public static readonly IReadOnlyDictionary<MessageTarget, GUIContent> routing =
                new Dictionary<MessageTarget, GUIContent>
                {
                    [MessageTarget.AuthorityOnly] = EditorGUIUtility.TrTextContentWithIcon("Send to Authority Only",
                        Icons.GetPath("Coherence.MessageTarget.AuthorityOnly")),
                    [MessageTarget.All] = EditorGUIUtility.TrTextContentWithIcon("Send to Everyone, including yourself",
                        Icons.GetPath("Coherence.MessageTarget.All")),
                };

            public static readonly IReadOnlyDictionary<PredictionMode, GUIContent> predictionWithTooltip =
                new Dictionary<PredictionMode, GUIContent>
                {
                    [PredictionMode.Never] =
                        EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Hierarchy.Simulated"),
                            "No Client Prediction"),
                    [PredictionMode.Always] =
                        EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Hierarchy.Networked"),
                            "Always Client Predict"),
                    [PredictionMode.InputAuthority] =
                        EditorGUIUtility.TrIconContent(Icons.GetPath("Coherence.Input.Remote"),
                            "Only Predict With Input Authority"),
                };

            public static readonly IReadOnlyDictionary<PredictionMode, GUIContent> predictionWithLabel =
                new Dictionary<PredictionMode, GUIContent>
                {
                    [PredictionMode.Never] = EditorGUIUtility.TrTextContentWithIcon("No Client Prediction",
                        Icons.GetPath("Coherence.Hierarchy.Simulated")),
                    [PredictionMode.Always] = EditorGUIUtility.TrTextContentWithIcon("Always Client Predict",
                        Icons.GetPath("Coherence.Hierarchy.Networked")),
                    [PredictionMode.InputAuthority] =
                        EditorGUIUtility.TrTextContentWithIcon("Only Predict With Input Authority",
                            Icons.GetPath("Coherence.Input.Remote")),
                };

            public static readonly GUIContent deprecated = EditorGUIUtility.TrIconContent("Warning");

            public static GUIContent cloneMode = EditorGUIUtility.TrTextContentWithIcon(
                "Clone Mode is enabled. Post-processing and operations such as baking are disabled. Modifying assets is not recommended.",
                Icons.GetPath("Coherence.Clone"));
            public static GUIContent allowEdits = EditorGUIUtility.TrTextContent("Allow Editing");
        }

        public class GUIStyles
        {
            public static readonly GUIStyle wrappedLabel = new(EditorStyles.label)
            {
                wordWrap = true,
            };

            public static readonly GUIStyle richLabel = new(wrappedLabel)
            {
                richText = true,
            };

            /// <summary>
            /// Like <see cref="richLabel"/>, but text color is pure white (on both Dark and Light themes).
            /// </summary>
            public static readonly GUIStyle richWhiteLabel = new(richLabel);

            public static readonly GUIStyle richLabelNoWrap = new(richLabel)
            {
                wordWrap = false,
            };

            public static readonly GUIStyle miniLabel = new(EditorStyles.miniLabel)
            {
                wordWrap = true,
            };

            public static readonly GUIStyle labelNoExpand = new(EditorStyles.label)
            {
                wordWrap = true,
                stretchWidth = false,
            };

            public static readonly GUIStyle richMiniLabel = new(miniLabel)
            {
                richText = true,
                wordWrap = true,
            };

            public static readonly GUIStyle richMiniLabelNoWrap = new(miniLabel)
            {
                richText = true,
                wordWrap = false,
            };

            public static readonly GUIStyle richToggle = new(EditorStyles.toggle)
            {
                richText = true,
            };

            public static readonly GUIStyle centeredGreyMiniLabelWrap = new(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true,
            };

            public static readonly GUIStyle centeredLabelWrap = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };

            public static readonly GUIStyle verticallyCenteredRowLabel = new(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(),
            };

            public static readonly GUIStyle centeredStretchedLabel = new(centeredGreyMiniLabelWrap)
            {
                stretchWidth = true,
                stretchHeight = true,
            };

            public static readonly GUIStyle centeredStretchedTinyLabel = new(centeredStretchedLabel)
            {
                fontSize = 6,
                fontStyle = FontStyle.Bold,
            };

            public static readonly GUIStyle miniLabelRight = new(richMiniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };

            public static readonly GUIStyle greyMiniLabelRight = new(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };

            public static readonly GUIStyle centeredLabelTopImage = new(centeredStretchedLabel)
            {
                imagePosition = ImagePosition.ImageAbove,
            };

            public static readonly GUIStyle centeredMiniLabel = new(richMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };

            public static readonly GUIStyle statusBar = new("ProjectBrowserBottomBarBg")
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 0, 0),
            };

            public static readonly GUIStyle miniLabelGrey = new(EditorStyles.miniLabel)
            {
                normal = new GUIStyleState
                {
                    textColor = Color.grey,
                },
                hover = new GUIStyleState
                {
                    textColor = Color.grey,
                },
            };

            public static readonly GUIStyle miniLabelGreyWrap = new(miniLabelGrey)
            {
                wordWrap = true,
            };

            public static readonly GUIStyle wordWrappedMiniLabelHighlight = new(EditorStyles.wordWrappedMiniLabel)
            {
                normal = new GUIStyleState
                {
                    textColor = Color.cyan,
                },
            };

            public static readonly GUIStyle miniButtonLeftPressed = new(EditorStyles.miniButtonLeft)
            {
                normal = EditorStyles.miniButtonLeft.hover,
            };

            public static readonly GUIStyle statusBarButton = new(EditorStyles.toolbarButton)
            {
                margin = new RectOffset(0, 0, 1, 0),
                padding = new RectOffset(
                    EditorStyles.toolbarButton.padding.left,
                    EditorStyles.toolbarButton.padding.right,
                    EditorStyles.toolbarButton.padding.top - 1,
                    EditorStyles.toolbarButton.padding.bottom),
            };

            public static readonly GUIStyle toolbarDropDownToggle = new("toolbarDropDownToggle");
            public static readonly GUIStyle toolbarDropDownToggleRight = new("toolbarDropDownToggleRight");
            public static readonly GUIStyle toolbarDropDownToggleButton = new("toolbarDropDownToggleButton");
            public static readonly GUIStyle toolbarDropDown = new("toolbarDropDown");
            public static readonly GUIStyle toolbarDropDownLeft = new("toolbarDropDownLeft");
            public static readonly GUIStyle toolbarDropDownRight = new("toolbarDropDownRight");

            public static readonly GUIStyle iconButton = new("IconButton")
            {
                stretchWidth = false,
            };

            public static readonly GUIStyle iconButtonPadded = new(iconButton)
            {
                margin = new RectOffset(0, 0, 2, 0),
            };

            public static readonly GUIStyle menuItem = new("MenuItem")
            {
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = 19,
            };

            public static readonly GUIStyle miniMenuItem = new("MenuItem")
            {
                padding = new RectOffset(menuItem.padding.left, menuItem.padding.right, 0, 0),
                fixedHeight = 14,
                fontSize = EditorStyles.miniLabel.fontSize,
            };

            public static readonly GUIStyle separator = new("sv_iconselector_sep");

            public static readonly GUIStyle frameBox = new("FrameBox");

            public static readonly GUIStyle tabOnlyOne = new("Tab onlyOne");
            public static readonly GUIStyle tabFirst = new("Tab first");
            public static readonly GUIStyle tabMiddle = new("Tab middle");
            public static readonly GUIStyle tabLast = new("Tab last");

            public static readonly GUIStyle boldButton = new(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
            };

            public static readonly GUIStyle header = new(EditorStyles.helpBox)
            {
                margin = new RectOffset(2, 2, 12, 2),
            };

            public static readonly GUIStyle toolbarButton = new(EditorStyles.toolbarButton)
            {
                stretchWidth = false,
                stretchHeight = false,
            };

            public static readonly GUIStyle bigButton = new(EditorStyles.miniButton)
            {
                fixedHeight = 32,
            };

            public static readonly GUIStyle bigBoldButton = new(bigButton)
            {
                fontStyle = FontStyle.Bold,
            };

            /// <summary>
            /// GUIStyle for a mini button just wide enough to contain the text "Save".
            /// </summary>
            public static readonly GUIStyle saveButton = new(EditorStyles.miniButton)
            {
                fixedWidth = 42,
                stretchWidth = false
            };

            public static readonly GUIStyle toolbarSearchCancel;
            public static readonly GUIStyle toolbarSearchCancelEmpty;

            static GUIStyles()
            {
                // At some point during 2021.3 LTS Unity renamed the internal style ToolbarSeachCancelButton to
                // ToolbarSearchCancelButton. For compatibility purposes, we default to the new name, but fallback to
                // the old one.
                toolbarSearchCancel = GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.FindStyle("ToolbarSeachCancelButton");
                toolbarSearchCancelEmpty = GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty") ?? GUI.skin.FindStyle("ToolbarSeachCancelButtonEmpty");

                richWhiteLabel.normal.textColor = Color.white;
                richWhiteLabel.hover.textColor = Color.white;
                richWhiteLabel.active.textColor = Color.white;
                richWhiteLabel.focused.textColor = Color.white;
            }
        }

        public static void BeginColorGroup(Color color)
        {
            colorStack.Push(GUI.color);
            GUI.color = color;
        }

        public static void EndColorGroup()
        {
            if (colorStack.Count > 0)
            {
                GUI.color = colorStack.Pop();
            }
        }

        public static void BeginBackgroundColorGroup(Color color)
        {
            backgroundColorStack.Push(GUI.color);
            GUI.backgroundColor = color;
        }

        public static void EndBackgroundColorGroup()
        {
            if (backgroundColorStack.Count > 0)
            {
                GUI.backgroundColor = backgroundColorStack.Pop();
            }
        }

        public static GUIContent GetInterpolationContent(Binding binding)
        {
            if (binding == null || !binding.interpolationSettings)
            {
                return GUIContents.interpolationNone;
            }

            return GetInterpolationContent(binding.interpolationSettings);
        }

        public static GUIContent GetInterpolationContent(InterpolationSettings settings)
        {
            if (!settings)
            {
                return GUIContents.interpolationNone;
            }

            return settings.interpolator switch
            {
                SplineInterpolator _ => GUIContents.interpolationCatmullRom,
                LinearInterpolator _ => GUIContents.interpolationLinear,
                _ => GUIContents.interpolationNone,
            };
        }

        public static Rect GetTabRect(Rect rect, int tabIndex, int tabCount, out GUIStyle tabStyle)
        {
            tabStyle = GUIStyles.tabMiddle;

            if (tabCount == 1)
            {
                tabStyle = GUIStyles.tabOnlyOne;
            }
            else if (tabIndex == 0)
            {
                tabStyle = GUIStyles.tabFirst;
            }
            else if (tabIndex == tabCount - 1)
            {
                tabStyle = GUIStyles.tabLast;
            }

            var tabWidth = rect.width / tabCount;
            var left = Mathf.RoundToInt(tabIndex * tabWidth);
            var right = Mathf.RoundToInt((tabIndex + 1) * tabWidth);
            return new Rect(rect.x + left, rect.y, right - left, 22);
        }

        public static bool TryGetSchemaTypeContent(SchemaType t, out GUIContent content)
        {
            return schemaTypeMap.TryGetValue(t, out content);
        }

        public static bool TryGetSchemaButtonTypeContent(SchemaType t, out GUIContent content)
        {
            return schemaTypeButtonMap.TryGetValue(t, out content);
        }

        public static GUIContent GetContent(Component component, Descriptor descriptor)
        {
            return EditorCache.GetContent(component, descriptor);
        }

        public static GUIContent GetInvalidContent(Descriptor descriptor, string reason)
        {
            return EditorGUIUtility.TrTextContent(descriptor.Signature, reason);
        }

        public static void DrawSelectableLabel(string text, GUIStyle guiStyle, bool honorIndent = false)
        {
            var indent = EditorGUI.indentLevel;
            if (!honorIndent)
            {
                EditorGUI.indentLevel = 0;
            }

            var content = EditorGUIUtility.TrTempContent(text);
            var width = guiStyle.CalcSize(content).x;
            var controlRect = EditorGUILayout.GetControlRect(false,
                guiStyle.CalcHeight(content, width),
                guiStyle, GUILayout.ExpandWidth(false));

            EditorGUI.SelectableLabel(controlRect, text, guiStyle);
            if (!honorIndent)
            {
                EditorGUI.indentLevel = indent;
            }
        }

        public static void DrawSelectableLabel(string text)
        {
            DrawSelectableLabel(text, EditorStyles.label);
        }

        public static void DrawSelectableLabel(string prefix, string text, GUIStyle guiStyle)
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(prefix, guiStyle, guiStyle);
            DrawSelectableLabel(text, guiStyle);
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawSelectableLabel(string prefix, string text, GUIStyle prefixStyle, GUIStyle guiStyle)
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(prefix, guiStyle, prefixStyle);
            DrawSelectableLabel(text, guiStyle);
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawSelectableLabel(string prefix, string text)
        {
            DrawSelectableLabel(prefix, text, EditorStyles.label);
        }

        public static void DrawSelectableLabel(GUIContent prefix, GUIContent content, GUIStyle guiStyle,
            GUIStyle prefixStyle)
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(prefix, guiStyle, prefixStyle);
            DrawSelectableLabel(content, guiStyle);
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawSelectableLabel(GUIContent prefix, GUIContent content, GUIStyle guiStyle)
        {
            DrawSelectableLabel(prefix, content, guiStyle, guiStyle);
        }

        public static void DrawSelectableLabel(GUIContent prefix, GUIContent content)
        {
            DrawSelectableLabel(prefix, content, EditorStyles.label, EditorStyles.label);
        }

        public static void DrawSelectableLabel(GUIContent content, GUIStyle guiStyle, bool honorIndent = false)
        {
            var indent = EditorGUI.indentLevel;
            if (!honorIndent)
            {
                EditorGUI.indentLevel = 0;
            }

            var s = guiStyle.CalcSize(content);
            EditorGUILayout.SelectableLabel(content.text, guiStyle, GUILayout.Height(s.y),
                GUILayout.ExpandWidth(false));
            if (!honorIndent)
            {
                EditorGUI.indentLevel = indent;
            }
        }

        public static bool DrawIndentedButton(string text)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.miniButton);
            rect = EditorGUI.IndentedRect(rect);
            return GUI.Button(rect, text);
        }

        public static bool DrawButton(string prefix, string text, GUIStyle guiStyle)
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(prefix, guiStyle, EditorStyles.label);
            var state = GUILayout.Button(text, guiStyle);
            EditorGUILayout.EndHorizontal();

            return state;
        }

        public static bool DrawButton(string prefix, string text)
        {
            return DrawButton(prefix, text, EditorStyles.miniButton);
        }

        public static bool DrawButton(GUIContent prefix, GUIContent text, GUIStyle guiStyle)
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(prefix, guiStyle, EditorStyles.label);
            var state = GUILayout.Button(text, guiStyle);
            EditorGUILayout.EndHorizontal();

            return state;
        }

        public static bool DrawButton(GUIContent prefix, GUIContent text)
        {
            return DrawButton(prefix, text, EditorStyles.miniButton);
        }

        public static void DrawSection(SerializedProperty serializedProperty, GUIContent content)
        {
            _ = EditorGUILayout.BeginVertical(GUI.skin.box);
            var headerLabelStyle = GUIStyles.miniLabelGreyWrap;
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, headerLabelStyle);
            content = EditorGUI.BeginProperty(rect, content, serializedProperty);

            EditorGUI.LabelField(rect, content, headerLabelStyle);

            EditorGUI.indentLevel++;

            using var it = serializedProperty.Copy();
            using var end = it.GetEndProperty();

            while (it.NextVisible(true) && !SerializedProperty.EqualContents(it, end))
            {
                _ = EditorGUILayout.PropertyField(it);
            }

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
            EditorGUILayout.EndVertical();
        }

        public static string DrawSearchField(string searchString, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight,
                EditorStyles.toolbarSearchField, options);
            var buttonRect = rect;
            buttonRect.xMin += buttonRect.width - 14f;

            if (!string.IsNullOrEmpty(searchString))
            {
                EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Arrow);
            }

            if (Event.current.type == EventType.MouseUp && buttonRect.Contains(Event.current.mousePosition))
            {
                searchString = string.Empty;
                GUI.changed = true;
                GUIUtility.keyboardControl = 0;
            }

            searchString = EditorGUI.TextField(rect, searchString, EditorStyles.toolbarSearchField);

            if (!string.IsNullOrEmpty(searchString))
            {
                if (GUI.Button(buttonRect, GUIContent.none,
                        !string.IsNullOrEmpty(searchString)
                            ? GUIStyles.toolbarSearchCancel
                            : GUIStyles.toolbarSearchCancelEmpty))
                {
                    searchString = string.Empty;
                    GUI.changed = true;
                    GUIUtility.keyboardControl = 0;
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape &&
                    GUIUtility.hotControl == 0)
                {
                    searchString = string.Empty;
                    GUI.changed = true;
                    Event.current.Use();
                }
            }

            return searchString;
        }

        public static bool DrawFoldout(bool foldout, GUIContent title, GUIContent body)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, title);
            if (foldout)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(body, GUIStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            return foldout;
        }

        public static GUIContent GetIcon(GameObject gameObject)
        {
            var isAsset = AssetDatabase.Contains(gameObject);

            var icon = isAsset
                ? AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(gameObject))
                : PrefabUtility.GetIconForGameObject(gameObject);

            // custom key to avoid clashing objects with same name (e.g. prefab in stage vs prefab in project window, with different icons)
            return EditorGUIUtility.TrTextContent($"{gameObject.name}||{gameObject.GetInstanceID()}", gameObject.name, "",
                icon);
        }

        public static void DrawCloneModeMessage(bool allowEditsInPlayMode = false)
        {
            if (!CloneMode.Enabled)
            {
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(GUIContents.cloneMode, GUIStyles.miniLabel);
            var enabled = Application.isPlaying && allowEditsInPlayMode;
            if (!enabled)
            {
                EditorGUILayout.Space();
                var value = EditorGUILayout.Toggle(GUIContents.allowEdits, CloneMode.AllowEdits);
                if (EditorGUI.EndChangeCheck())
                {
                    CloneMode.AllowEdits = value;
                }
            }
            EditorGUILayout.EndVertical();
        }

        // A toggle that returns true on mouse down - like a popup button and returns true if checked
        public static bool DrawDropDownToggle(ref bool toggled, GUIContent content, GUIStyle toggleStyle)
        {
            var buttonStyle = GUIStyle.none;

            // This is to be compatible with existing code
            if (toggleStyle == GUIStyles.toolbarDropDownToggle ||
                toggleStyle == GUIStyles.toolbarDropDownToggleRight)
            {
                buttonStyle = GUIStyles.toolbarDropDownToggleButton;
            }

            return DrawDropDownToggle(ref toggled, content, toggleStyle, buttonStyle);
        }

        public static bool DrawDropDownToggle(ref bool toggled, GUIContent content, GUIStyle toggleStyle, GUIStyle toggleDropdownButtonStyle)
        {
            var toggleRect = GUILayoutUtility.GetRect(content, toggleStyle);
            Rect arrowRightRect;

            if (toggleDropdownButtonStyle != null)
            {
                arrowRightRect = new Rect(toggleRect.xMax - toggleDropdownButtonStyle.fixedWidth - toggleDropdownButtonStyle.margin.right, toggleRect.y, toggleDropdownButtonStyle.fixedWidth, toggleRect.height);
            }
            else
            {
                arrowRightRect = new Rect(toggleRect.xMax - toggleStyle.padding.right, toggleRect.y, toggleStyle.padding.right, toggleRect.height);
            }


            var clicked = EditorGUI.DropdownButton(arrowRightRect, GUIContent.none, FocusType.Passive, GUIStyle.none);

            if (!clicked)
            {
                toggled = GUI.Toggle(toggleRect, toggled, content, toggleStyle);
            }

            // Ensure that the dropdown button is rendered on top of the toggle
            if (Event.current.type == EventType.Repaint && toggleDropdownButtonStyle != null && toggleDropdownButtonStyle != GUIStyle.none)
            {
                EditorGUI.DropdownButton(arrowRightRect, GUIContent.none, FocusType.Passive, toggleDropdownButtonStyle);
            }

            return clicked;
        }

        public static void DrawHelpButton(string link)
        {
            Debug.Assert(!string.IsNullOrEmpty(link));
            if (GUILayout.Button(GUIContents.help, GUIStyles.iconButtonPadded))
            {
                Application.OpenURL(link);
            }
        }

        public static void DrawHelpButton(DocumentationKeys key)
        {
            var link = DocumentationLinks.GetDocsUrl(key);
            DrawHelpButton(link);
        }
    }
}
