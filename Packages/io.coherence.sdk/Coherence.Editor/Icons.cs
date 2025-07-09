// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;

    internal static class Icons
    {
        public static GUIContent GetContent(string iconName, string tooltip = null)
        {
            return EditorGUIUtility.TrIconContent(GetPath(iconName), tooltip);
        }

        public static GUIContent GetContentWithText(string iconName, string text, string tooltip = null)
        {
            return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, GetPath(iconName));
        }

        public static string GetPath(string name)
        {
            return $"{Paths.iconsPath}/{name}.png";
        }

        public static Texture2D GetIconTexture(string iconName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GetPath(iconName));
        }
    }
}

