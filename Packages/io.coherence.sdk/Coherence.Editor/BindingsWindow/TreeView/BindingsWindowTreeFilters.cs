// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using System.Linq;
    using Coherence.Editor.Toolkit;
    using UnityEditor.IMGUI.Controls;

    internal class BindingsWindowTreeFilters
    {
        private class GUIContents
        {
            public static readonly GUIContent filterPopupButton = EditorGUIUtility.TrIconContent("scenevis_visible_hover", "Select which types are visible to edit");
            public static readonly GUIContent filterPopupButtonMixed = EditorGUIUtility.TrIconContent("scenevis_visible-mixed_hover", "Select which types are visible to edit");
        }
        private static class FilterPopupSettings
        {
            public const float componentRowHeight = 19;
            public const float bottomPadding = 4;
            public const float separatorHeight = 4;
            public const float windowWidth = 150;
        }
        private class GUIStyles
        {
            public static readonly GUIStyle separator = new GUIStyle("sv_iconselector_sep");
            public static readonly GUIStyle menuItem = new GUIStyle("MenuItem")
            {
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = FilterPopupSettings.componentRowHeight,
            };
            public static readonly GUIStyle menuItemButton = new GUIStyle(menuItem)
            {
                padding = new RectOffset(4, 4, 1, 2),
            };
        }

        internal bool PopupOpen { private set; get; }

        internal string SearchString { get; private set; } = "";
        internal Dictionary<SchemaType, bool> FilterTypes;
        private Rect popupRect;
        private BindingsWindow editingWindow;
        private SearchField searchField;

        internal BindingsWindowTreeFilters(BindingsWindow editingWindow)
        {
            this.editingWindow = editingWindow;
            SearchString = "";
            FilterTypes = new Dictionary<SchemaType, bool>();
            ClearTypeFilters();
            searchField = new SearchField();
        }

        internal void SetSearchString(string newString)
        {
            SearchString = newString;
        }

        internal void SetFilter(SchemaType type, bool active)
        {
            FilterTypes[type] = active;
        }

        internal bool AnyFilterActive()
        {
            if (AnyTypeFiltersActive() || !string.IsNullOrEmpty(SearchString))
            {
                return true;
            }

            return false;
        }

        internal bool AnyTypeFiltersActive()
        {
            if (FilterTypes != null)
            {
                return FilterTypes.Values.ToList().Contains(true);
            }

            return false;
        }
        internal bool AllTypeFiltersActive()
        {
            if (FilterTypes != null)
            {
                return !(FilterTypes.Values.ToList().Contains(false));
            }

            return false;
        }

        internal void ClearTypeFilters()
        {
            var schemaTypes = UIHelpers.GetSchemaTypeByDisplayOrder();
            foreach (SchemaType type in schemaTypes)
            {
                FilterTypes[type] = false;
            }
        }

        internal void SetAllTypeFilters()
        {
            var schemaTypes = UIHelpers.GetSchemaTypeByDisplayOrder();
            foreach (SchemaType type in schemaTypes)
            {
                FilterTypes[type] = true;
            }
        }
        internal bool FilterOutBinding(SchemaType type, string text)
        {
            if (AnyFilterActive())
            {
                if (!string.IsNullOrEmpty(SearchString))
                {
                    return !text.Contains(SearchString);
                }

                return FilterTypes[type];
            }

            return false;
        }

        // Drawing functions
        internal void DrawFilters()
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();

            string searchString = searchField.OnToolbarGUI(SearchString);

            if (!editingWindow.StateController.Methods)
            {
                DrawTypeFilters();
            }

            if (EditorGUI.EndChangeCheck()) {
                SetSearchString(searchString);
                editingWindow.RefreshTree(true);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTypeFilters() {
            var content = AnyTypeFiltersActive() ? GUIContents.filterPopupButtonMixed  : GUIContents.filterPopupButton;
            if (EditorGUILayout.DropdownButton(content, FocusType.Passive, ContentUtils.GUIStyles.toolbarDropDown, GUILayout.ExpandWidth(false)))
            {
                var popup = new GenericPopup(OnPopupFilterGUI, GetPopupFilterSize, OnPopupOpened, OnPopupClosed);
                PopupWindow.Show(popupRect, popup);
                GUIUtility.ExitGUI();
            }

            if (Event.current.type == EventType.Repaint)
            {
                popupRect = GUILayoutUtility.GetLastRect();
                popupRect.x -= FilterPopupSettings.windowWidth - popupRect.width;
            }
        }

        private void OnPopupFilterGUI()
        {
            _ = EditorGUILayout.BeginVertical();
            DrawPopupFilterSelectors();
            DrawPopupFilterSeparator();
            DrawPopupFilterTypes();
            EditorGUILayout.EndVertical();
        }

        private void OnPopupOpened()
        {
            PopupOpen = true;
        }
        private void OnPopupClosed()
        {
            PopupOpen = false;
        }

        private void DrawPopupFilterSelectors()
        {
            EditorGUI.BeginDisabledGroup(!AnyTypeFiltersActive());
            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Show All", "scenevis_visible_hover"), GUIStyles.menuItemButton))
            {
                ClearTypeFilters();
                Repaint();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(AllTypeFiltersActive());
            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Hide All", "scenevis_hidden_hover"), GUIStyles.menuItemButton))
            {
                SetAllTypeFilters();
                Repaint();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPopupFilterSeparator()
        {
            GUILayout.Space(FilterPopupSettings.separatorHeight);
            GUILayout.Label(GUIContent.none, GUIStyles.separator);
        }
        private void DrawPopupFilterTypes()
        {
            var schemaTypes = UIHelpers.GetSchemaTypeByDisplayOrder();

            for (int i = 0; i < schemaTypes.Length; i++)
            {
                SchemaType type = schemaTypes[i];

                if (type == SchemaType.Unknown)
                {
                    continue;
                }
                
                _ = ContentUtils.TryGetSchemaButtonTypeContent(type, out GUIContent content);
                bool isActive = !FilterTypes[type];

                EditorGUI.BeginChangeCheck();
                var active = GUILayout.Toggle(isActive, content, GUIStyles.menuItem, GUILayout.Width(FilterPopupSettings.windowWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    SetFilter(type, !active);
                    Repaint();
                }
            }
        }

        private void Repaint()
        {
            editingWindow.Refresh(true);
            editingWindow.RefreshTree(true);
        }

        private Vector2 GetPopupFilterSize()
        {
            return new Vector2(FilterPopupSettings.windowWidth, 200);
        }
    }
}
