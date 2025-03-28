using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimOffsetProperty : AttachmentComponentPropertyBase<WeaponAdsOffset>
    {
        public Vector3 TargetOffset;
        public Vector3 TargetEulerOffset;

        WeaponAdsOffset.Override target;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            target = Component.AddOverride();
        }

        public override void Update()
        {
            if (target != null)
            {
                target.TargetPosition = TargetOffset;
                target.TargetRotation = Quaternion.Euler(TargetEulerOffset);
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            target.Dispose();
        }
    }
}
