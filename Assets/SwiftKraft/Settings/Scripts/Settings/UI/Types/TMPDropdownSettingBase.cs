using TMPro;
using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    public class TMPDropdownSettingBase : ComponentSettingBase<SingleSetting<int>, TMP_Dropdown>
    {
        [SerializeField]
        private string _id;

        public override string ID => _id;

        public int Value
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
            Component.SetValueWithoutNotify(Value);
        }

        public virtual void OnValueChanged(int value) => Value = value;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Component.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}
