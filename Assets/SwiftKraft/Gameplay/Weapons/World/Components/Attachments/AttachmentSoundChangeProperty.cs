using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentSoundChangeProperty : WeaponAttachmentSlot.AttachmentProperty
    {
        public string Action;
        public List<WeaponAudio.Audio.Clip> Clips = new();

        WeaponAudio.Audio weaponAudio;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            WeaponAudio au = this.parent.parent.GetComponentInParent<WeaponAudio>();
            if (au != null)
                weaponAudio = au.Sounds.FirstOrDefault((a) => a.Action == Action);
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
