using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    public class FullscreenToggleSetting : ToggleSettingBase
    {
        protected override void OnUpdate()
        {
            base.OnUpdate();
            SetFullscreen(Value);
        }

        protected override Startup GetStartup() => new Begin(Data);

        public static void SetFullscreen(bool value) => Screen.fullScreen = value;

        public class Begin : Startup
        {
            public Begin(Setting parent) : base(parent) { }

            public override void Start()
            {
                if (Parent is SingleSetting<bool> setting)
                    SetFullscreen(setting.Value);
            }
        }
    }
}
