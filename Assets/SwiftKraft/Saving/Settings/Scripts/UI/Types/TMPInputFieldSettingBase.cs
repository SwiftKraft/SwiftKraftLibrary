using TMPro;
using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    public class TMPInputFieldSettingBase : ComponentSettingBase<SingleSetting<string>, TMP_InputField>
    {
        public override string ID => _id;

        [SerializeField]
        private string _id;

        public string Value
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
            Component.SetTextWithoutNotify(Value);
        }

        public virtual void OnValueChanged(string value) => Value = value;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Component.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}