using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Saving.Settings.UI
{
    public class ProfileResetter : MonoBehaviour
    {
        Button button;

        private void Awake()
        {
            button = GetComponentInChildren<Button>();
            button.onClick.AddListener(ResetProfile);
        }

        private void OnDestroy() => button.onClick.RemoveListener(ResetProfile);

        public void ResetProfile() => SettingsManager.ResetProfile();
    }
}
