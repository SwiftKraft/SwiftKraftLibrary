// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using Prefs;

    /// <summary>
    /// Represents a <see langword="bool"/> setting that is persisted using a specified solution
    /// (<see cref="UnityEditorPrefs"/> by default).
    /// </summary>
    internal sealed class BoolSetting : ISetting<bool>
    {
        public event Action<bool> Changed;

        private static readonly UnityEditorPrefs DefaultStorage = new();

        private readonly string key;
        private readonly bool defaultValue;
        private bool autoSave;
        private bool value;
        private readonly IPrefsImplementation storage;

        public bool Value
        {
            get => !autoSave ? value : Value = GetValueFromStorage();

            set
            {
                if (value == this.value && (!autoSave || value == GetValueFromStorage()))
                {
                    return;
                }

                this.value = value;

                if (autoSave)
                {
                    Save();
                }

                Changed?.Invoke(value);
            }
        }

        public bool HasUnsavedChanges => value != GetValueFromStorage();

        /// <summary>
        /// When <see cref="AutoSave"/> is enabled, the boolean value is read from and written directly to the storage;
        /// otherwise, the value is cached locally and only written to the storage when <see cref="Save"/> is called.
        /// </summary>
        public bool AutoSave
        {
            get => autoSave;

            set
            {
                autoSave = value;

                if (value)
                {
                    Save();
                }
            }
        }

        public BoolSetting(string key, bool defaultValue = false, bool autoSave = true, IPrefsImplementation storage = null)
        {
            this.key = key;
            this.defaultValue = defaultValue;
            this.autoSave = autoSave;
            this.storage = storage ?? DefaultStorage;
            this.value = defaultValue;
            Value = GetValueFromStorage();
        }

        public void Toggle() => Value = !Value;

        public void Save()
        {
            if (value == defaultValue)
            {
                storage.DeleteKey(key);
            }
            else
            {
                storage.SetBool(key, value);
            }
        }

        public void Discard() => value = GetValueFromStorage();

        private bool GetValueFromStorage() => storage.GetBool(key, defaultValue);

        public static implicit operator bool(BoolSetting boolean) => boolean?.Value ?? false;
        public static bool operator !(BoolSetting boolean) => boolean is null || !boolean.Value;
        public static bool operator ==(BoolSetting boolean, bool value) => boolean?.Equals(value) ?? false;
        public static bool operator !=(BoolSetting boolean, bool value) => !boolean?.Equals(value) ?? false;
        public bool Equals(BoolSetting other) => other is not null && string.Equals(key, other.key);
        public bool Equals(bool value) => Value.Equals(value);
        public override bool Equals(object obj) => obj is BoolSetting other && Equals(other);
        public override int GetHashCode() => key.GetHashCode();
        public override string ToString() => $"{key}:{Value}";
    }
}
