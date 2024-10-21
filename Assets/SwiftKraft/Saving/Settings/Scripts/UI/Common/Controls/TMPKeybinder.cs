using System;
using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    public class TMPKeybinder : TMPButtonSettingBase<SingleSetting<KeyCode>>
    {
        public const string RebindText = "Rebinding...";
        public const KeyCode StopRebindKey = KeyCode.Escape;

        public static TMPKeybinder Rebinding { get; private set; }

        public KeyCode Keybind
        {
            get => Data.Value;
            set => Data.Value = value;
        }

        public override void OnClick()
        {
            base.OnClick();

            if (Rebinding != null)
                return;

            Rebinding = this;
            Interactable = false;
            Text.SetText(RebindText);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Text.SetText(Keybind.ToString());
        }

        private void Update()
        {
            if (Rebinding != this)
                return;

            if (Input.anyKeyDown)
            {
                if (!Input.GetKeyDown(StopRebindKey))
                    Keybind = GetKeyPressed();

                OnUpdate();
                Rebinding = null;
                Interactable = true;
            }
        }

        public static KeyCode GetKeyPressed()
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(key))
                    return key;
            return KeyCode.None;
        }
    }
}
