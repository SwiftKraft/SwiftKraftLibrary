// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor
{
    using UnityEngine;

    internal static class SettingGUIExtensions
    {
        public static void DrawButton(this BoolSetting setting, GUIContent label, GUIStyle style, Color activeColor)
        {
            GUI.contentColor = setting ? activeColor : Color.white;
            try
            {
                if (GUILayout.Button(label, style))
                {
                    setting.Toggle();
                }
            }
            finally
            {
                GUI.contentColor = Color.white;
            }
        }
    }
}
