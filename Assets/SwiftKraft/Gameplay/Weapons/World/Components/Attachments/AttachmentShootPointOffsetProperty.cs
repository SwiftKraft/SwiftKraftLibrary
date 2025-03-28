using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentShootPointOffsetProperty : AttachmentComponentPropertyBase<WeaponShootPoint>
    {
        public Vector3 TargetOffset;

        WeaponShootPoint.Override target;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            target = Component.AddOverride();
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
