using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimOffsetProperty : WeaponAttachmentSlot.AttachmentProperty
    {
        public Vector3 TargetOffset;
        public Vector3 TargetEulerOffset;

        WeaponAdsOffset.Override target;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            target = this.parent.parent.GetComponentInParent<WeaponAdsOffset>().AddOverride();
        }

        public override void Update()
        {
            if (target != null)
            {
                target.TargetPosition = TargetOffset;
                target.TargetRotation = Quaternion.Euler(TargetEulerOffset);
            }
        }

        public override void Uninstall()
        {
            base.Uninstall();
            if (target != null)
            {
                target.TargetPosition = Vector3.zero;
                target.TargetRotation = new(0f, 0f, 0f, 1f);
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            target.Dispose();
        }
    }
}
