using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    /// <summary>
    /// Initializes all UI setting components.
    /// </summary>
    public class SettingsActivator : MonoBehaviour
    {
        /// <summary>
        /// The list of registered UI settings.
        /// </summary>
        public readonly List<SettingBase> Registered = new();

        private void Awake()
        {
            SettingBase[] settings = GetComponentsInChildren<SettingBase>(true);
            Registered.AddRange(settings);
            foreach (SettingBase setting in Registered)
                setting.Init(this);
        }
    }
}
