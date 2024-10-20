using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Saving.Settings.UI
{
    public class ProfileDeleter : MonoBehaviour
    {
        Button button;

        private void Awake()
        {
            button = GetComponentInChildren<Button>();
            button.onClick.AddListener(Delete);

            SettingsManager.OnProfileChange += OnProfileChange;

            OnProfileChange();
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(Delete);

            SettingsManager.OnProfileChange -= OnProfileChange;
        }

        private void OnProfileChange() => button.interactable = !SettingsManager.Current.Name.Equals(SettingsManager.DefaultProfileName);

        public void Delete() => SettingsManager.DeleteProfile();
    }
}
