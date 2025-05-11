using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public class RotateTowardsImmobileMotor : ImmobileMotor
    {
        public float RotateSpeed = 360f;

        public override Quaternion Interpolation() => Quaternion.RotateTowards(CurrentLookRotation, WishLookRotation, RotateSpeed * Time.fixedDeltaTime);
    }
}
