// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Coherence.Toolkit;

    internal class BindingsTreeViewIntHelper
    {
        internal static long DrawField(Rect rect, long value, BindingsTreeViewInput.Type type, int step,
            bool selected, bool hasWarning)
        {
            BindingsTreeViewInput.SetColorBeforeInputControl(type, selected, hasWarning, step, Color.white);
            string text = EditorGUI.DelayedTextField(rect, GUIContent.none, value.ToString(), EditorStyles.numberField);
            BindingsTreeViewInput.EvaluateFocus(type, step, selected);
            GUI.contentColor = Color.white;

            if (!long.TryParse(text, out long input))
            {
                return value;
            }

            return input;
        }
    }
}
