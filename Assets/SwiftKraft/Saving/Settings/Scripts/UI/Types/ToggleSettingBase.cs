using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Saving.Settings.UI
{
    public class ToggleSettingBase : ComponentSettingBase<SingleSetting<bool>, Toggle>
    {
        [SerializeField]
        private string _id;

        public override string ID => _id;

        public bool Value
        {
            get => Data.Value;
            set => Data.Value = value;
        }

        public override void Init(SettingsActivator activator)
        {
            base.Init(activator);
            Component.onValueChanged.AddListener(OnValueChanged);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Component.SetIsOnWithoutNotify(Value);
        }

        public virtual void OnValueChanged(bool value) => Value = value;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Component.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}
