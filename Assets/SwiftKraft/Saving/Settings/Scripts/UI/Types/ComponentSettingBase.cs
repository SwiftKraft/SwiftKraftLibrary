using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    /// <summary>
    /// Base class for a component based setting UI.
    /// </summary>
    /// <typeparam name="T0">The setting type.</typeparam>
    /// <typeparam name="T1">The component type.</typeparam>
    public abstract class ComponentSettingBase<T0, T1> : SettingBase<T0> where T0 : Setting, new() where T1 : Component
    {
        /// <summary>
        /// The referenced component, gets and caches on demand.
        /// </summary>
        public T1 Component
        {
            get
            {
                if (_component == null)
                    _component = GetComponentInChildren<T1>(true);
                return _component;
            }
        }
        T1 _component;
    }
}
