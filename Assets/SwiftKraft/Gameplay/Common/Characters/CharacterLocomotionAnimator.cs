using SwiftKraft.Gameplay.Interfaces;
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
        public Quaternion RotationOffset = Quaternion.identity;

        public Vector2 TargetDirection { get; set; }
        public Vector2 CurrentDirection { get; private set; }

        public Vector2[] StateMultipliers;

        [Header("Grounding")]
        public string Grounded = "Grounded";

        public MotorBase Motor { get; private set; }

        public IGroundable GroundableCache { get; private set; }

        public float CurrentGrounded { get; private set; }

        Vector2 vel;
        float velGround;

        private void Awake()
        {
            Motor = GetComponentInParent<MotorBase>();
            GroundableCache = Motor as IGroundable;
        }

        private void Update()
        {
            if (Motor != null)
            {
                Vector3 localDir = RotationOffset * Quaternion.Inverse(transform.rotation) * Motor.WishMoveDirection;
                TargetDirection = new Vector2(localDir.x, localDir.z).CircleToSquare() * GetStateMultiplier(Motor.State);
            }

            CurrentDirection = (CurrentDirection - TargetDirection).magnitude <= 0.0001f
                ? TargetDirection
                : Vector2.SmoothDamp(CurrentDirection, TargetDirection, ref vel, SmoothTime);

            CurrentGrounded = Mathf.SmoothDamp(CurrentGrounded, GroundableCache.IsGrounded ? 1f : 0f, ref velGround, SmoothTime);

            Component.SetFloatSafe(XMovement, CurrentDirection.x);
            Component.SetFloatSafe(ZMovement, CurrentDirection.y);

            if ((Object)GroundableCache != null)
                Component.SetFloatSafe(Grounded, CurrentGrounded);
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
