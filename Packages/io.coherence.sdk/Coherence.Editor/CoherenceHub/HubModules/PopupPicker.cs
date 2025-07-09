// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;
    using UnityEngine.Events;

    public class PopupPicker<T> : PopupWindowContent where T : class
    {
        private const float RightPadding = 20;
        private readonly T[] availableItems;
        private readonly Func<T, string> displayFunction;
        private readonly int maxRows;
        private readonly float minWidth;
        private T[] filteredItems;
        private SearchField searchField;
        private string searchText;
        private Vector2 pos;

        private Color evenColor;
        private Color oddColor;
        private Color lightEvenColor;
        private Color lightOddColor;
        private Color lightSelectedColor;
        private Color darkSelectedColor;

        private const int MaxResults = 50;
        private const float bottomPadding = 5f;
        private const string EvenColor = "#3f3f3f";
        private const string OddColor = "#383838";
        private const string LightEvenColor = "#c8c8c8";
        private const string LightOddColor = "#cacaca";
        private const string LightSelectedColor = "#29abe2";
        private const string DarkSelectedColor = "#2c5d87";

        public UnityAction<T> ItemSelected;

        public PopupPicker(T[] items, int maxRows = 10, float width = 200, Func<T, string> display = null)
        {
            this.availableItems = items;
            this.maxRows = maxRows;
            minWidth = width;
            displayFunction = display ?? (item => item.ToString());
            filteredItems = items.OrderBy(displayFunction).ToArray();
            searchField = new SearchField();
            ColorUtility.TryParseHtmlString(EvenColor, out evenColor);
            ColorUtility.TryParseHtmlString(OddColor, out oddColor);
            ColorUtility.TryParseHtmlString(LightEvenColor, out lightEvenColor);
            ColorUtility.TryParseHtmlString(LightOddColor, out lightOddColor);
            ColorUtility.TryParseHtmlString(LightSelectedColor, out lightSelectedColor);
            ColorUtility.TryParseHtmlString(DarkSelectedColor, out darkSelectedColor);
        }

        public override Vector2 GetWindowSize()
        {
            var maxRowCount = Math.Min(filteredItems.Length, maxRows) + 1;
            var height = (EditorGUIUtility.singleLineHeight + 2f) * maxRowCount + bottomPadding;
            return new Vector2(GetWidth(), height);
        }

        private float GetWidth() =>
            availableItems.Length == 0
                ? minWidth
                : Mathf.Max(minWidth, availableItems.Select(CalcSize).Max() + RightPadding);

        private float CalcSize(T item) => EditorStyles.label.CalcSize(new GUIContent(displayFunction(item))).x;

        public override void OnGUI(Rect rect)
        {
            UpdateFilteredItems(searchField.OnGUI(searchText));

            pos = EditorGUILayout.BeginScrollView(pos, GUILayout.MaxHeight(GetWindowSize().y));

            for (var i = 0; i < filteredItems.Length; i++)
            {
                var item = filteredItems[i];
                var controlRect = EditorGUILayout.GetControlRect(false);
                var isSelected = Event.current.type != EventType.Layout && MouseInRect(controlRect);

                var bgColor = GetRowBackgroundColor(i % 2 == 0);
                var selectedColor = EditorGUIUtility.isProSkin ? darkSelectedColor : lightSelectedColor;
                bgColor = isSelected ? selectedColor : bgColor;

                EditorGUI.DrawRect(controlRect, bgColor);
                EditorGUI.LabelField(controlRect, $"{displayFunction(item)}");

                if (MouseInRect(controlRect) & Event.current.type == EventType.MouseUp)
                {
                    OnItemSelected(item);
                    editorWindow.Close();
                }

                if (isSelected)
                {
                    editorWindow.Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdateFilteredItems(string newSearchText)
        {
            if (string.Equals(newSearchText, searchText))
            {
                return;
            }

            searchText = newSearchText;
            FilterItems(searchText);
        }

        private void FilterItems(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                filteredItems = availableItems
                    .Where(item => displayFunction(item).ToLower().Contains(text.ToLower()))
                    .Take(MaxResults)
                    .OrderBy(displayFunction).ToArray();
            }
            else
            {
                filteredItems = availableItems.OrderBy(displayFunction).ToArray();
            }
        }

        private Color GetRowBackgroundColor(bool isEven)
        {
            var (even, odd) = GetRowColors();
            var bgColor = isEven ? even : odd;
            return bgColor;
        }

        private (Color, Color) GetRowColors() =>
            EditorGUIUtility.isProSkin ? (evenColor, oddColor) : (lightEvenColor, lightOddColor);

        private void OnItemSelected(T item) => ItemSelected?.Invoke(item);

        private static bool MouseInRect(Rect controlRect) => controlRect.Contains(Event.current.mousePosition);
    }
}

