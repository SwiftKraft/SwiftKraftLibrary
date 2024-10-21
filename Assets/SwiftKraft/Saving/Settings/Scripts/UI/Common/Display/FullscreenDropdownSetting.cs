using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    public class FullscreenDropdownSetting : TMPDropdownSettingBase
    {
        public override void Init(SettingsActivator activator)
        {
            List<string> options = new() { "Exclusive Fullscreen", "Borderless Window", "Maximized Window", "Windowed" };

            Component.ClearOptions();
            Component.AddOptions(options);

            base.Init(activator);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            SetFullscreenMode((FullScreenMode)Value);
        }

        protected override Startup GetStartup() => new Begin(Data);

        public static void SetFullscreenMode(FullScreenMode mode) => Screen.fullScreenMode = mode;

        public class Begin : Startup
        {
            public Begin(Setting parent) : base(parent) { }

            public override void Start()
            {
                base.Start();
                if (Parent is SingleSetting<int> setting)
                    SetFullscreenMode((FullScreenMode)setting.Value);
            }
        }
    }
}
