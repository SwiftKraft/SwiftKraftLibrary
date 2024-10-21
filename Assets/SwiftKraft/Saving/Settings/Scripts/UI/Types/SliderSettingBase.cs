using SwiftKraft.Saving.Settings.UI.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwiftKraft.Saving.Settings.UI
{
    public class SliderSettingBase : ComponentSettingBase<SingleSetting<float>, Slider>
    {
        protected SliderEvents ExtraSliderEvents { get; private set; }

        public string TextFormat = "0.00";

        public TMP_Text Text
        {
            get
            {
                if (_text == null)
                    _text = Component.GetComponentInChildren<TMP_Text>();
                return _text;
            }
        }
        TMP_Text _text;

        public override string ID => _id;

        [SerializeField]
        private string _id;

        public float Value
        {
            get => Data.Value;
            set => Data.Value = value;
        }

        public override void Init(SettingsActivator activator)
        {
            base.Init(activator);
            Component.onValueChanged.AddListener(OnValueChanged);
            ExtraSliderEvents = Component.GetComponent<SliderEvents>();

            if (ExtraSliderEvents == null)
                ExtraSliderEvents = Component.gameObject.AddComponent<SliderEvents>();

            ExtraSliderEvents.OnReleaseSlider += OnReleaseSlider;
        }

        protected virtual void OnReleaseSlider(PointerEventData data) => base.OnUpdate();

        protected override void OnUpdate()
        {
            Component.SetValueWithoutNotify(Value);
            Text.SetText(Component.value.ToString(TextFormat));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Component.onValueChanged.RemoveListener(OnValueChanged);
            ExtraSliderEvents.OnReleaseSlider -= OnReleaseSlider;
        }

        protected virtual void OnValueChanged(float value) => Value = value;
    }
}
