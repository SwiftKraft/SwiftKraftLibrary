using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Saving.Settings.UI
{
    public class ResolutionSetting : TMPDropdownSettingBase
    {
        public readonly static List<Vector2Int> Resolutions = new();

        public override void Init(SettingsActivator activator)
        {
            UpdateResolutions();

            List<string> temp = new();
            foreach (Vector2Int vec in Resolutions)
                temp.Add(vec.x + "x" + vec.y);

            Component.ClearOptions();
            Component.AddOptions(temp);

            base.Init(activator);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            SetResolution(Value);
        }

        protected override Startup GetStartup() => new Begin(Data);

        public static void SetResolution(int index)
        {
            Debug.Log("Setting Resolution: " + index);

            if (index < 0 || index >= Resolutions.Count)
                return;

            Screen.SetResolution(Resolutions[index].x, Resolutions[index].y, Screen.fullScreenMode);
        }

        public static void UpdateResolutions()
        {
            Resolutions.Clear();

            foreach (Resolution res in Screen.resolutions)
            {
                Vector2Int dims = new(res.width, res.height);
                if (!Resolutions.Contains(dims))
                    Resolutions.Add(dims);
            }

            Resolutions.Reverse();
        }

        public class Begin : Startup
        {
            public Begin(Setting parent) : base(parent) { }

            public override void Start()
            {
                base.Start();
                if (Parent is SingleSetting<int> setting)
                    SetResolution(setting.Value);
            }
        }
    }
}
