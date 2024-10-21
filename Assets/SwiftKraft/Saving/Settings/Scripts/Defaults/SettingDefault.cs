using UnityEngine;

namespace SwiftKraft.Saving.Settings.Defaults
{
    /// <summary>
    /// Base class for a setting default value override.
    /// </summary>
    public abstract class SettingDefault : ScriptableObject
    {
        /// <summary>
        /// The ID for the setting.
        /// </summary>
        [field: SerializeField]
        public string ID { get; private set; }
    }

    /// <summary>
    /// Base class for a setting default value override, but the type of value is specified.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    public abstract class SettingDefault<T> : SettingDefault
    {
        /// <summary>
        /// The default value.
        /// </summary>
        [field: SerializeField]
        public T Default { get; private set; }
    }
}
