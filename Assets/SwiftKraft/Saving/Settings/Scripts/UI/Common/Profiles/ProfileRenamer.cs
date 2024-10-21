using TMPro;

namespace SwiftKraft.Saving.Settings.UI
{
    public class ProfileRenamer : SettingBase
    {
        TMP_InputField input;

        public override void Init(SettingsActivator activator)
        {
            base.Init(activator);
            input = GetComponentInChildren<TMP_InputField>();
            UpdateInput();

            input.onEndEdit.AddListener(Rename);
            input.onSubmit.AddListener(Rename);
        }

        protected override void OnProfileChange()
        {
            base.OnProfileChange();
            UpdateInput();
        }

        public void UpdateInput()
        {
            input.SetTextWithoutNotify(SettingsManager.Current.Name);
            input.interactable = !SettingsManager.Current.Name.Equals(SettingsManager.DefaultProfileName);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            input.onEndEdit.RemoveListener(Rename);
            input.onSubmit.RemoveListener(Rename);
        }

        private void Rename(string name)
        {
            if (SettingsManager.Global.Profiles.Contains(name))
            {
                UpdateInput();
                return;
            }

            SettingsManager.RenameProfile(name);
        }
    }
}
