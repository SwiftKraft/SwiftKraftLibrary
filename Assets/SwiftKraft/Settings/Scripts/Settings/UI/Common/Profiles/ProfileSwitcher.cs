using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Saving.Settings.UI
{
    public class ProfileSwitcher : MonoBehaviour
    {
        Button createButton;
        TMP_Dropdown dropdown;

        private void Awake()
        {
            createButton = GetComponentInChildren<Button>();
            dropdown = GetComponentInChildren<TMP_Dropdown>();

            createButton.onClick.AddListener(CreateProfile);
            dropdown.onValueChanged.AddListener(OnValueChanged);

            SettingsManager.CheckDefault();
            SettingsManager.Global.OnProfilesUpdated += UpdateOptions;

            UpdateOptions();
        }

        private void OnDestroy()
        {
            createButton.onClick.RemoveListener(CreateProfile);
            dropdown.onValueChanged.RemoveListener(OnValueChanged);

            SettingsManager.Global.OnProfilesUpdated -= UpdateOptions;
        }

        public void CreateProfile()
        {
            string name = "New Profile " + SettingsManager.Global.Profiles.Count;
            SettingsManager.CreateProfile(name);
            SettingsManager.LoadProfile(name);
            UpdateOptions();
        }

        public void OnValueChanged(int value)
        {
            string text = dropdown.options[value].text;
            SettingsManager.LoadProfile(text);
        }

        public void UpdateOptions()
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(SettingsManager.Global.Profiles);
            SetCurrent();
        }

        public void SetCurrent()
        {
            for (int i = 0; i < dropdown.options.Count; i++)
                if (dropdown.options[i].text.Equals(SettingsManager.Global.SelectedProfileName))
                {
                    dropdown.SetValueWithoutNotify(i);
                    break;
                }
        }
    }
}
