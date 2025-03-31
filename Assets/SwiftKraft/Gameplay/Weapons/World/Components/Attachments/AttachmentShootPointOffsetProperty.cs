using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentShootPointOffsetProperty : AttachmentOverridePropertyBase<WeaponShootPoint, WeaponShootPoint.Override>
    {
        public Vector3 TargetOffset;

        public override void ApplyOverrides() => overrider.OverridePosition = TargetOffset;

        public override WeaponShootPoint.Override CreateOverrider() => Component.AddOverride();

        public override void Uninstall()
        {
            base.Uninstall();
            overrider.OverridePosition = default;
        }

        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentShootPointOffsetProperty()
            {
                TargetOffset = TargetOffset,
            };
    }
}
