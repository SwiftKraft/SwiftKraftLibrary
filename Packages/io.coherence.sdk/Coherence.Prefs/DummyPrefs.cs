// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Prefs
{
    public class DummyPrefs : IPrefsImplementation
    {
        public void Save() { }
        public void DeleteAll() { }
        public void DeleteKey(string key) { }
        public bool HasKey(string key) => false;
        public void SetBool(string key, bool value) { }
        public bool GetBool(string key, bool defaultValue) => false;
        public void SetFloat(string key, float value) { }
        public float GetFloat(string key) => 0f;
        public float GetFloat(string key, float defaultValue) => defaultValue;
        public void SetInt(string key, int value) { }
        public int GetInt(string key) => 0;
        public int GetInt(string key, int defaultValue) => defaultValue;
        public void SetString(string key, string value) { }
        public string GetString(string key) => null;
        public string GetString(string key, string defaultValue) => defaultValue;
    }
}
