using SwiftKraft.Saving.Settings.UI.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwiftKraft.Saving.Settings.UI
{
    public class SliderSettingBase : ComponentSettingBase<SingleSetting<float>, Slider>
    {
        protected SliderEvents ExtraSliderEvents;

        public string TextFormat = "0.00";

        public TMP_Text Text
        {
            get
            {
                if (_text == null)
                    _text = InputField == null || InputField.placeholder is not TMP_Text text ? Component.GetComponentInChildren<TMP_Text>() : text;
                return _text;
            }
        }
        TMP_Text _text;

        public TMP_InputField InputField
        {
            get
            {
                if (_inputField == null)
                    _inputField = Component.GetComponentInChildren<TMP_InputField>();
                return _inputField;
            }
        }
        TMP_InputField _inputField;

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

            if (InputField != null)
            {
                InputField.onEndEdit.AddListener(OnInputFieldChanged);
                InputField.SetTextWithoutNotify(Value.ToString(TextFormat));
            }

            if (!Component.TryGetComponent(out ExtraSliderEvents))
                ExtraSliderEvents = Component.gameObject.AddComponent<SliderEvents>();

            ExtraSliderEvents.OnReleaseSlider += OnReleaseSlider;
        }

        protected virtual void OnReleaseSlider(PointerEventData data) => base.OnUpdate();

        protected override void OnUpdate()
        {
            Component.SetValueWithoutNotify(Value);

            if (Text != null)
                Text.SetText(Component.value.ToString(TextFormat));

            if (InputField != null)
                InputField.SetTextWithoutNotify(Value.ToString(TextFormat));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Component.onValueChanged.RemoveListener(OnValueChanged);

            if (InputField != null)
                InputField.onEndEdit.RemoveListener(OnInputFieldChanged);

            ExtraSliderEvents.OnReleaseSlider -= OnReleaseSlider;
        }

        protected virtual void OnInputFieldChanged(string value)
        {
            if (float.TryParse(value, out float res))
                OnValueChanged(res);
            else
                InputField.SetTextWithoutNotify(Value.ToString(TextFormat));
        }

        protected virtual void OnValueChanged(float value) => Value = value;
    }
}
