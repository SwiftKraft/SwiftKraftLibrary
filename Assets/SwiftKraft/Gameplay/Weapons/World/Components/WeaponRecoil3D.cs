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

        public float MaxRecoverSpeed = 10f;

        Vector3 curVelRot;
        Vector3 curVelPos;

        protected override void DecayRecoil()
        {
            float smoothTime = 1f / (DecayRate.EvaluateSafe(Heat.CurrentValue) * DecayMultiplier * RecoverMultiplier);

            Rotation = Rotation.SmoothDamp(Quaternion.Euler(Vector3.zero), ref curVelRot, smoothTime, MaxRecoverSpeed);
            Position = Vector3.SmoothDamp(Position, Vector3.zero, ref curVelPos, smoothTime, MaxRecoverSpeed);
        }

        protected override void ApplyRecoil()
        {
            Vector2 move = new Vector2(-Vertical.EvaluateSafe(Heat.CurrentValue) * VerticalMultiplier, Horizontal.EvaluateSafe(Heat.CurrentValue) * HorizontalMultiplier) * RecoilMultiplier;
            Rotation *= Quaternion.Euler(move * (!Smooth ? 1f : Time.fixedDeltaTime));
            Position += Positional.Evaluate(Heat.CurrentValue) * (PositionalMultiplier * (!Smooth ? 1f : Time.fixedDeltaTime));
        }
    }
}
