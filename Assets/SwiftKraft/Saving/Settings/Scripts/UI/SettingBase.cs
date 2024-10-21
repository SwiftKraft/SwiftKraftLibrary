using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    /// <summary>
    /// Base class for a UI setting, can inherit functions that are called by events from the SettingsManager.
    /// </summary>
    public abstract class SettingBase : MonoBehaviour
    {
        public SettingsActivator Activator { get; private set; }

        /// <summary>
        /// Called when the SettingsActivator calls Awake().
        /// </summary>
        /// <param name="activator"></param>
        public virtual void Init(SettingsActivator activator)
        {
            Activator = activator;
            SettingsManager.OnProfileChange += OnProfileChange;
        }

        /// <summary>
        /// Called when the current setting profile changes.
        /// </summary>
        protected virtual void OnProfileChange() { }

        protected virtual void OnDestroy() => SettingsManager.OnProfileChange -= OnProfileChange;
    }

    /// <summary>
    /// Base class for basic settings, has more utility functions.
    /// </summary>
    /// <typeparam name="T">The type of setting it controls.</typeparam>
    public abstract class SettingBase<T> : SettingBase where T : Setting, new()
    {
        /// <summary>
        /// The ID of the setting.
        /// </summary>
        public abstract string ID { get; }

        /// <summary>
        /// The saved setting data.
        /// </summary>
        public T Data;

        public override void Init(SettingsActivator activator)
        {
            base.Init(activator);
            Load();
        }

        /// <summary>
        /// Loads the value from the setting ID, will add one if the setting doesn't exist.
        /// </summary>
        public virtual void Load()
        {
            if (!SettingsManager.Current.TryGetSetting(ID, out Data) && !SettingsManager.Current.TryAddSetting(ID, out Data))
                Debug.LogError("Failed to load and create new setting data for " + gameObject.name);

            Data.Startup ??= GetStartup();
            Data.OnUpdate += OnUpdate;
            Data.OnReset += OnReset;
            OnUpdate();
        }

        /// <summary>
        /// Provides the setting a startup function. Meant to be overridden and used for custom needs.
        /// </summary>
        /// <returns>A startup object.</returns>
        protected virtual Startup GetStartup() => null;

        protected override void OnProfileChange()
        {
            base.OnProfileChange();
            Data.OnUpdate -= OnUpdate;
            Data.OnReset -= OnReset;
            Load();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Data.OnUpdate -= OnUpdate;
            Data.OnReset -= OnReset;
        }

        /// <summary>
        /// Called when the setting updates its value.
        /// </summary>
        protected virtual void OnUpdate()
        {
            if (!SettingsManager.Current.Resetting && Activator.Initialized)
                SettingsManager.SaveProfile();
        }

        /// <summary>
        /// Called when the setting resets.
        /// </summary>
        protected virtual void OnReset() { }
    }
}
