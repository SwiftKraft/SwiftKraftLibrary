// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;

    internal class PrecisionPopupDrawer
    {
        private static int PopupWidth = 300;
        private static int PopupHeight = 60;

        private static double? Precision;

        protected static BindingLODStepData cachedData;
        protected static BindingsWindow BindingsWindow;

        public static double Draw(Rect rect, BindingArchetypeData archetypeData, BindingLODStepData lodStep,
            BindingsWindow bindingsWindow, string unit = "")
        {
            BindingsWindow = bindingsWindow;

            if (GUI.Button(rect, GUIContent.none, EditorStyles.textArea))
            {
                var r = new Rect(rect.x, rect.yMax, 0,0);
                cachedData = lodStep;
                Precision = cachedData.Precision;
                ShowPopup(r, archetypeData, lodStep);
            }

            GUIStyle miniText = new GUIStyle(EditorStyles.miniLabel);
            miniText.alignment = TextAnchor.MiddleLeft;
            miniText.richText = true;
            miniText.fontSize = 9;
            Rect multiplierrect = new Rect(rect.x, rect.y, rect.width, rect.height);

            if (lodStep.Precision > 1f)
            {
                GUI.Label(multiplierrect, $"<color=#888888>+/-</color>{lodStep.Precision:G3}{unit}", miniText);
            }
            else
            {
                GUI.Label(multiplierrect, $"<color=#888888>+/-</color>{lodStep.Precision:G2}{unit}", miniText);
            }

            if (lodStep != cachedData || !Precision.HasValue)
            {
                return -1;
            }

            var precision = Precision.Value;
            Precision = null;
            return precision;
        }

        private static void ShowPopup(Rect rect, BindingArchetypeData archetypeData, BindingLODStepData lodStep)
        {
            var popup = new GenericPopup(() => OnPopupGUI(archetypeData, lodStep), GetPopupSize, OnPopupOpen, OnPopupClose);
            PopupWindow.Show(rect, popup);
            GUIUtility.ExitGUI();
        }

        private static void OnPopupOpen()
        {
        }

        private static void OnPopupGUI(BindingArchetypeData archetypeData, BindingLODStepData data)
        {
            _ = EditorGUILayout.BeginVertical();

            GUILayout.Space(2);

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.richText = true;
            GUILayout.Label($"<color=#888888>Precision:</color> +/-{cachedData.Precision}", style, GUILayout.ExpandWidth(true));
            GUILayout.Space(2);

            GUILayout.Label(GUIContent.none, ContentUtils.GUIStyles.separator);

            double maxPrecision = ArchetypeMath.GetRoundedPrecisionByBitsAndRange(32, archetypeData.TotalRange);
            GUILayout.BeginHorizontal();

            double value = 1f / Mathf.Pow(10, 1);
            for (int i = 1; value >= maxPrecision; i++)
            {
                string title = value.ToString();

                GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
                buttonStyle.fontSize = 9;

                EditorGUI.BeginChangeCheck();
                var sel = GUILayout.Button(title, buttonStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    SetPrecision(value);
                    GenericPopup.Repaint();
                    GUIUtility.ExitGUI();
                }
                value = 1f / Mathf.Pow(10, i + 1);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void SetPrecision(double value)
        {
            Precision = value;
            BindingsWindow.Repaint();
        }

        private static void OnPopupClose()
        {
        }

        private static Vector2 GetPopupSize()
        {
            return new Vector2(PopupWidth, PopupHeight);
        }
    }
}
