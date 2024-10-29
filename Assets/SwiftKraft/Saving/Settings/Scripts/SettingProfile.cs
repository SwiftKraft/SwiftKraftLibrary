using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Saving.Settings
{
    /// <summary>
    /// A profile containing settings set by the user with their IDs.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn, IsReference = false)]
    public class SettingProfile
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        [JsonProperty]
        public string Name;

        /// <summary>
        /// The list of settings that are set.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, Setting> Settings = new();

        /// <summary>
        /// Whether this profile is resetting.
        /// </summary>
        public bool Resetting { get; private set; }

        /// <summary>
        /// Resets all settings to default value.
        /// </summary>
        public void Reset()
        {
            Resetting = true;
            foreach (Setting setting in Settings.Values)
                setting.Reset();
            Resetting = false;
            SettingsManager.SaveProfile();
        }

        /// <summary>
        /// Gets a setting of provided type.
        /// </summary>
        /// <typeparam name="T">The setting type.</typeparam>
        /// <param name="key">The ID of the setting.</param>
        /// <returns>The acquired setting.</returns>
        public T GetSetting<T>(string key) where T : Setting => Settings.ContainsKey(key) && Settings[key] is T t ? t : null;

        /// <summary>
        /// Gets a setting of provided type.
        /// </summary>
        /// <typeparam name="T">The setting type.</typeparam>
        /// <param name="key">The ID of the setting.</param>
        /// <param name="setting">The acquired setting.</param>
        /// <returns>Whether the acquisition is successful.</returns>
        public bool TryGetSetting<T>(string key, out T setting) where T : Setting
        {
            setting = GetSetting<T>(key);
            return setting != null;
        }

        /// <summary>
        /// Adds a setting of provided type.
        /// </summary>
        /// <typeparam name="T">The setting type.</typeparam>
        /// <param name="key">The ID of the setting.</param>
        /// <returns>The newly created setting.</returns>
        public T AddSetting<T>(string key) where T : Setting, new()
        {
            if (!Settings.ContainsKey(key))
            {
                T t = new() { ID = key };
                Settings.Add(key, t);
                t.Reset();
                return t;
            }

            return null;
        }

        /// <summary>
        /// Adds a setting of provided type.
        /// </summary>
        /// <typeparam name="T">The setting type.</typeparam>
        /// <param name="key">The ID of the setting.</param>
        /// <param name="setting">The newly created setting.</param>
        /// <returns>Whether the creation of the setting is successful.</returns>
        public bool TryAddSetting<T>(string key, out T setting) where T : Setting, new()
        {
            setting = AddSetting<T>(key);
            return setting != null;
        }

        /// <summary>
        /// Used in-game to get settings even if they aren't initialized.
        /// </summary>
        /// <typeparam name="T">The setting type.</typeparam>
        /// <param name="key">The ID of the setting.</param>
        /// <param name="setting">The resulting setting, nullable.</param>
        /// <returns>Whether it is successful or not.</returns>
        public bool TrySetting<T>(string key, out T setting) where T : Setting, new() => TryGetSetting(key, out setting) || TryAddSetting(key, out setting);
    }

    public delegate void SettingUpdate();
    public delegate void SettingReset();

    /// <summary>
    /// Base class for running a start function on game start.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn, IsReference = false)]
    public abstract class Startup
    {
        [JsonProperty]
        public Setting Parent { get; private set; }

        public Startup(Setting parent) => Parent = parent;

        public virtual void Start() => Parent.Reset();
    }

    /// <summary>
    /// Base class for a setting, has basic events to subscribe to.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Setting
    {
        /// <summary>
        /// The ID of the setting.
        /// </summary>
        [JsonProperty]
        public string ID { get; set; }

        public SettingUpdate OnUpdate;
        public SettingReset OnReset;

        /// <summary>
        /// Resets the setting into its default.
        /// </summary>
        public virtual void Reset() { OnReset?.Invoke(); }

        [JsonProperty]
        public Startup Startup;
    }

    /// <summary>
    /// A setting for one value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class SingleSetting<T> : Setting
    {
        /// <summary>
        /// The stored value. Will send events when set.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnUpdate?.Invoke();
            }
        }

        [JsonProperty]
        T _value;

        public override void Reset()
        {
            Value = SettingsLoader.Instance.GetDefault<T>(ID);
            base.Reset();
        }
    }
}
