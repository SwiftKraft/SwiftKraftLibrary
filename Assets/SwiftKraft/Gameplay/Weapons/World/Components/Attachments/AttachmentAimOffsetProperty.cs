using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimOffsetProperty : AttachmentOverridePropertyBase<WeaponAdsOffset, WeaponAdsOffset.Override>
    {
        public Vector3 TargetOffset;
        public Vector3 TargetEulerOffset;

        public override WeaponAdsOffset.Override CreateOverrider() => Component.AddOverride();

        public override void ApplyOverrides()
        {
            overrider.TargetPosition = TargetOffset;
            overrider.TargetRotation = Quaternion.Euler(TargetEulerOffset);
        }
    }
}
