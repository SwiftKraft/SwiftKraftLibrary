using SwiftKraft.Saving.Settings.Defaults;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Saving.Settings
{
    /// <summary>
    /// A singleton class for executing the startup functions and providing default values.
    /// </summary>
    public class SettingsLoader : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of SettingsLoader. Cached on demand.
        /// </summary>
        public static SettingsLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SettingsLoader>();

                    if (_instance == null)
                        Debug.LogError("Settings Loader not found! ");
                }

                return _instance;
            }

            private set => _instance = value;
        }
        static SettingsLoader _instance;

        /// <summary>
        /// The default value override scriptable objects.
        /// </summary>
        public List<SettingDefault> Defaults = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            DontDestroyOnLoad(gameObject);

            foreach (Setting setting in SettingsManager.Current.Settings.Values)
                setting.Startup?.Start();
        }

        /// <summary>
        /// Gets the default value of a setting ID, if there is an override for it.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="id">The ID of the setting.</param>
        /// <returns>The default value.</returns>
        public T GetDefault<T>(string id)
        {
            foreach (SettingDefault setting in Defaults)
                if (setting.ID.Equals(id) && setting is SettingDefault<T> def)
                    return def.Default;
            return default;
        }
    }
}
