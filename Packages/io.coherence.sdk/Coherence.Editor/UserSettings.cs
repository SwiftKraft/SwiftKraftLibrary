// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;

    internal static class UserSettings
    {
        public static bool HasKey(string key)
        {
            return EditorUserSettings.GetConfigValue(key) == null;
        }

        public static void RemoveKey(string key)
        {
            EditorUserSettings.SetConfigValue(key, null);
        }

        public static void SetBool(string key, bool value)
        {
            EditorUserSettings.SetConfigValue(key, value ? "1" : "0");
        }

        public static bool GetBool(string key, bool defaultValue)
        {
            var v = EditorUserSettings.GetConfigValue(key);
            return v == null ? defaultValue : v == "1";
        }

        public static bool GetBool(string key)
        {
            return GetBool(key, false);
        }

        public static string GetString(string key, string defaultValue)
        {
            var v = EditorUserSettings.GetConfigValue(key);
            return string.IsNullOrEmpty(v) ? defaultValue : v;
        }

        public static string GetString(string key)
        {
            return GetString(key, string.Empty);
        }

        public static void SetString(string key, string value)
        {
            EditorUserSettings.SetConfigValue(key, value);
        }

        public static int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        public static int GetInt(string key, int defaultValue)
        {
            var v = EditorUserSettings.GetConfigValue(key);
            if (string.IsNullOrEmpty(v))
            {
                return defaultValue;
            }
            else
            {
                return int.TryParse(v, out int i) ? i : defaultValue;
            }
        }

        public static void SetInt(string key, int v)
        {
            EditorUserSettings.SetConfigValue(key, v.ToString());
        }
    }
}
