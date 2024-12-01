using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponRecoil3D : WeaponRecoil
    {
        public float RecoverMultiplier = 1f;

        public AnimationCurve Vertical;
        public AnimationCurve Horizontal;

        protected override void DecayRecoil(Transform recoil) => recoil.localRotation = Quaternion.RotateTowards(recoil.localRotation, Quaternion.Euler(Vector3.zero), DecayRate.Evaluate(Heat.CurrentValue) * DecayMultiplier * RecoverMultiplier * Time.fixedDeltaTime);

        protected override void ApplyRecoil(Transform recoil)
        {
            Vector2 move = new Vector2(-Vertical.Evaluate(Heat.CurrentValue), Horizontal.Evaluate(Heat.CurrentValue)) * RecoilMultiplier;
            recoil.Rotate(move, Space.Self);
        }
    }
}
