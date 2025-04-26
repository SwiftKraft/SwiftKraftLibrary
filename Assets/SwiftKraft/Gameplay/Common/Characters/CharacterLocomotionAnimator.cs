using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.Characters
{
    [RequireComponent(typeof(Animator))]
    public class CharacterLocomotionAnimator : RequiredDependencyComponent<Animator>
    {
        public string XMovement = "MoveX";
        public string ZMovement = "MoveZ";
        public float SmoothTime = 0.1f;

        public Vector2 TargetDirection { get; set; }
        public Vector2 CurrentDirection { get; private set; }

        public Vector2[] StateMultipliers;

        public MotorBase Motor { get; private set; }

        Vector2 vel;

        private void Awake() => Motor = GetComponentInParent<MotorBase>();

        private void Update()
        {
            if (Motor != null)
            {
                Vector3 localDir = Motor.LocalWishMoveDirection;
                TargetDirection = new Vector2(Mathf.Round(localDir.x), Mathf.Round(localDir.z)) * GetStateMultiplier(Motor.State);
            }

            CurrentDirection = (CurrentDirection - TargetDirection).magnitude <= 0.0001f
                ? TargetDirection
                : Vector2.SmoothDamp(CurrentDirection, TargetDirection, ref vel, SmoothTime);

            Component.SetFloatSafe(XMovement, CurrentDirection.x);
            Component.SetFloatSafe(ZMovement, CurrentDirection.y);
        }

        public float GetStateMultiplier(int state)
        {
            foreach (Vector2 item in StateMultipliers)
            {
                if (item.x == state)
                    return item.y;
            }
            return 1f;
        }
    }
}
