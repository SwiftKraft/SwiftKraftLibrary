// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;

    internal static class UIHelpers
    {
        public static Color BarLeftColor = Color.cyan;
        public static Color BarRightColor = Color.blue;
        private static Color NoValuecolor = Color.grey;

        public static void DrawCenteredLabel(string labelText, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(labelText, options);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static Vector2 GetMaxSizeOfLabels(GUISkin skin, List<string> labels)
        {
            Vector2 maxSize = Vector2.zero;
            foreach (string label in labels)
            {
                Vector2 labelSize = skin.label.CalcSize(new GUIContent(label));
                maxSize.x = Mathf.Max(maxSize.x, labelSize.x);
                maxSize.y = Mathf.Max(maxSize.y, labelSize.y);
            }

            return maxSize;
        }

        public static Color GetLODColor(float level, int maxLevel, bool active)
        {
            return active ? HSVLerp(BarLeftColor, BarRightColor, (float)level / maxLevel) : NoValuecolor;
        }

        public static Color HSVLerp(Color from, Color to, float amount)
        {
            Color.RGBToHSV(from, out float fromH, out float fromS, out float fromV);
            Color.RGBToHSV(to, out float toH, out float toS, out float toV);

            return Color.HSVToRGB(Mathf.Lerp(fromH, toH, amount), Mathf.Lerp(fromS, toS, amount),
                Mathf.Lerp(fromV, toV, amount));
        }

        public static void DrawIcon(string typeName, float size)
        {
            Texture icon = GetIcon(typeName);
            GUILayout.Label(icon, GUILayout.Height(size), GUILayout.Width(size));
        }

        public static Texture GetIcon(string typeName)
        {
            Texture icon = EditorGUIUtility.ObjectContent(null, Type.GetType(typeName)).image;
            if (icon == null)
            {
                icon = EditorGUIUtility.IconContent("cs Script Icon").image;
            }

            return icon;
        }

        public static class BackgroundStyle
        {
            private static GUIStyle style = new GUIStyle();
            private static Texture2D texture = new Texture2D(1, 1);

            public static GUIStyle Get(string textureName)
            {
                string path = $"{Coherence.Editor.Paths.uiAssetsPath}/{textureName}.png";
                Texture2D texture = EditorGUIUtility.FindTexture(path);
                style.normal.background = texture;
                return style;
            }
        }

        public static class Button
        {
            public static GUIStyle Get(string textureName, int border)
            {
                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.border = new RectOffset(border, border, border, border);
                string path = $"{Coherence.Editor.Paths.uiAssetsPath}/{textureName}.png";
                Texture2D texture = EditorGUIUtility.FindTexture(path);
                style.normal.background = texture;
                return style;
            }
        }

        public static bool LinkLabel(GUIContent label, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect position = GUILayoutUtility.GetRect(label, style, options);

            Handles.BeginGUI();
            Handles.color = style.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            return GUI.Button(position, label, style);
        }

        public static SchemaType[] GetSchemaTypeByDisplayOrder()
        {
            return Enum.GetValues(typeof(SchemaType)).Cast<SchemaType>().ToArray();
        }
    }
}
