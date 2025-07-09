// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor
{
    using System.Runtime.CompilerServices;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Extension methods for <see cref="GUIStyle"/> that simplify common operations related to drawing labels,
    /// which can be prone to errors if done manually.
    /// </summary>
    internal static class GUIStyleExtensions
    {
        private static readonly GUILayoutOption[] oneLayoutOption = new GUILayoutOption[1];

        /// <summary>
        /// Draws a label.
        /// </summary>
        /// <param name="style"> Style information (color, etc) for displaying the label. </param>
        /// <param name="content"> The label to draw. </param>
        /// <returns> Rectangle on the screen where the label was drawn. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect DrawLabel(this GUIStyle style, GUIContent content)
        {
            var rect = style.GetControlRect(content);
            GUI.Label(rect, content, style);
            return rect;
        }

        /// <summary>
        /// Draws a label.
        /// </summary>
        /// <param name="style"> Style information (color, etc) for displaying the label. </param>
        /// <param name="content"> The label to draw. </param>
        /// <param name="maxWidth">
        /// Maximum width on screen that the drawn label can occupy.
        /// <para>
        /// If the available space is not wide enough to fit the while label in one line, and word wrapping is active
        /// for the style, then the content will get drawn in multiple lines.
        /// </para>
        /// </param>
        /// <returns> Rectangle on the screen where the label was drawn. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect DrawLabel(this GUIStyle style, GUIContent content, float maxWidth)
        {
            var rect = style.GetControlRect(content, maxWidth);
            if (rect.width < EditorGUIUtility.singleLineHeight)
            {
                rect.x = Mathf.Max(rect.x, 0f);
                return rect;
            }

            EditorGUI.LabelField(rect, content, style);
            return rect;
        }

        /// <summary>
        /// Gets a rect that is just large enough to draw the <paramref name="content"/> with the given <paramref name="style"/>.
        /// <para>
        /// This method ensures that the last letter of the label won't wrap to a second line due to loss of precision
        /// due to rounding, even if word wrapping is active for the style.
        /// </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect GetControlRect(this GUIStyle style, GUIContent content)
        {
            oneLayoutOption[0] = GUILayout.Width(style.CalcSizeCeil(content).x);
            return EditorGUILayout.GetControlRect(false, oneLayoutOption);
        }

        /// <summary>
        /// Gets a rect that is just large enough to draw the <paramref name="content"/> with the given <paramref name="style"/>,
        /// if the rect is no wider than <paramref name="maxWidth"/>; otherwise, returns a rect for drawing the
        /// <paramref name="content"/> with a width of <paramref name="maxWidth"/>.
        /// <para>
        /// This method ensures that the last letter of the label won't wrap to a second line due to loss of precision
        /// due to rounding, even if word wrapping is active for the style.
        /// </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect GetControlRect(this GUIStyle style, GUIContent content, float maxWidth)
        {
            oneLayoutOption[0] = GUILayout.Width(style.CalcSizeCeil(content, maxWidth).x);
            return EditorGUILayout.GetControlRect(false, oneLayoutOption);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 CalcSizeCeil(this GUIStyle style, GUIContent content)
        {
            var size = style.CalcSize(content);
            size.x = Mathf.Ceil(size.x);
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 CalcSizeCeil(this GUIStyle style, GUIContent content, float maxWidth)
        {
            var size = style.CalcSizeCeil(content);
            if (size.x > maxWidth)
            {
                size.x = maxWidth;
            }

            return size;
        }
    }
}
