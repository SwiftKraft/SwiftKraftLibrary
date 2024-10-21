using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Saving.Settings.UI
{
    public abstract class TMPButtonSettingBase<T> : ComponentSettingBase<T, Button> where T : Setting, new()
    {
        public bool Interactable
        {
            get => Component.interactable;
            set => Component.interactable = value;
        }

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

        public override void Init(SettingsActivator activator)
        {
            base.Init(activator);
            Component.onClick.AddListener(OnClick);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Component.onClick.RemoveListener(OnClick);
        }

        public virtual void OnClick() { }
    }
}
