using System.Collections.Generic;
using System.Linq;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentSoundChangeProperty : AttachmentComponentPropertyBase<WeaponAudio>
    {
        public string Action;
        public List<WeaponAudio.Audio.Clip> Clips = new();

        WeaponAudio.Audio weaponAudio;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            if (Component != null)
                weaponAudio = Component.Sounds.FirstOrDefault((a) => a.Action == Action);
        }

        public override void Update()
        {
            if (weaponAudio != null)
                weaponAudio.Override = Clips;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            if (weaponAudio != null)
                weaponAudio.Override = null;
        }
    }
}
