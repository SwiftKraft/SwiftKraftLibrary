using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.UI;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class WeaponAttachmentMenu : MenuBase
    {
        public WeaponAttachments Target
        {
            get => _target;
            set
            {
                _target = value;
                UpdateUI();
            }
        }
        WeaponAttachments _target;

        public void UpdateUI()
        {

        }
    }
}
