using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentShootPointOffsetProperty : WeaponAttachmentSlot.AttachmentProperty
    {
        public Vector3 TargetOffset;

        WeaponShootPoint.Override target;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            target = this.parent.parent.GetComponentInParent<WeaponShootPoint>().AddOverride();
        }

        public override void Update()
        {
            if (target != null)
                target.OverridePosition = TargetOffset;
        }

        public override void Destroy()
        {
            base.Destroy();
            target.Dispose();
        }
    }
}
