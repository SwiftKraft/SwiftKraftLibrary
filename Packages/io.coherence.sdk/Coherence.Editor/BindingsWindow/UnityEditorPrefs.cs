// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor
{
    using Prefs;
    using UnityEditor;

    internal sealed class UnityEditorPrefs : IPrefsImplementation
    {
        public void Save() { }
        public void DeleteAll() => EditorPrefs.DeleteAll();
        public void DeleteKey(string key) => EditorPrefs.DeleteKey(key);
        public bool HasKey(string key) => EditorPrefs.HasKey(key);
        public void SetBool(string key, bool value) => EditorPrefs.SetBool(key, value);
        public bool GetBool(string key, bool defaultValue) => EditorPrefs.GetBool(key, defaultValue);
        public void SetFloat(string key, float value) => EditorPrefs.SetFloat(key, value);
        public float GetFloat(string key) => EditorPrefs.GetFloat(key);
        public float GetFloat(string key, float defaultValue) => EditorPrefs.GetFloat(key, defaultValue);
        public void SetInt(string key, int value) => EditorPrefs.SetInt(key, value);
        public int GetInt(string key) => EditorPrefs.GetInt(key);
        public int GetInt(string key, int defaultValue) => EditorPrefs.GetInt(key, defaultValue);
        public void SetString(string key, string value) => EditorPrefs.SetString(key, value);
        public string GetString(string key) => EditorPrefs.GetString(key);
        public string GetString(string key, string defaultValue) => EditorPrefs.GetString(key, defaultValue);
    }
}
