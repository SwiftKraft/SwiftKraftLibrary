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
        public ModifiableStatistic PositionalMultiplier = new();
        public Vector3Curve Positional;

        Vector3 curVelRot;
        Vector3 curVelPos;

        protected override void DecayRecoil()
        {
            float smoothTime = 1f / (DecayRate.Evaluate(Heat.CurrentValue) * DecayMultiplier * RecoverMultiplier);

            Rotation = Rotation.SmoothDamp(Quaternion.Euler(Vector3.zero), ref curVelRot, smoothTime);
            Position = Vector3.SmoothDamp(Position, Vector3.zero, ref curVelPos, smoothTime);
        }

        protected override void ApplyRecoil()
        {
            Vector2 move = new Vector2(-Vertical.Evaluate(Heat.CurrentValue) * VerticalMultiplier, Horizontal.Evaluate(Heat.CurrentValue) * HorizontalMultiplier) * RecoilMultiplier;
            Rotation *= Quaternion.Euler(move * (!Smooth ? 1f : Time.fixedDeltaTime));
            Position += Positional.Evaluate(Heat.CurrentValue) * (PositionalMultiplier * (!Smooth ? 1f : Time.fixedDeltaTime));
        }
    }
}
