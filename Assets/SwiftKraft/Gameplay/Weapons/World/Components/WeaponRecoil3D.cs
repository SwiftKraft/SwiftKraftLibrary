using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponRecoil3D : WeaponRecoil
    {
        public ModifiableStatistic RecoverMultiplier = new();

        public ModifiableStatistic VerticalMultiplier = new();
        public AnimationCurve Vertical;
        public ModifiableStatistic HorizontalMultiplier = new();
        public AnimationCurve Horizontal;

        Vector3 curVel;

        protected override void DecayRecoil(Transform recoil) => recoil.localRotation = recoil.localRotation.SmoothDamp(Quaternion.Euler(Vector3.zero), ref curVel, 1f / (DecayRate.Evaluate(Heat.CurrentValue) * DecayMultiplier * RecoverMultiplier));

        protected override void ApplyRecoil(Transform recoil)
        {
            Vector2 move = new Vector2(-Vertical.Evaluate(Heat.CurrentValue) * VerticalMultiplier, Horizontal.Evaluate(Heat.CurrentValue) * HorizontalMultiplier) * RecoilMultiplier;
            recoil.Rotate(move * (!Smooth ? 1f : Time.fixedDeltaTime), Space.Self);
        }
    }
}
