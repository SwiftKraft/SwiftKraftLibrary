// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Prefs
{
    public interface IPrefsImplementation
    {
        void Save();
        void DeleteAll();
        void DeleteKey(string key);
        bool HasKey(string key);
        void SetBool(string key, bool value);
        bool GetBool(string key, bool defaultValue);
        void SetFloat(string key, float value);
        float GetFloat(string key);
        float GetFloat(string key, float defaultValue);

        void SetInt(string key, int value);
        int GetInt(string key);
        int GetInt(string key, int defaultValue);

        void SetString(string key, string value);
        string GetString(string key);
        string GetString(string key, string defaultValue);
    }
}
