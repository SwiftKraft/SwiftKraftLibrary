using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Playables
{
    public class PlayableLocomotion : MonoBehaviour
    {
        public PlayableAnimationState State;

        public int Layer = 0;
        public int MoveX = 0;
        public int MoveY = 1;

        public float SmoothTime = 0.1f;
        public Vector3 RotationOffset;

        public Vector2 TargetDirection { get; set; }
        public Vector2 CurrentDirection { get; private set; }

        public Vector2[] StateMultipliers;

        [Header("Grounding")]
        public int Grounded = 2;

        public MotorBase Motor { get; private set; }
        public PlayableAnimationController Animator { get; private set; }

        public float CurrentGrounded { get; private set; }
        protected IGroundable GroundableCache { get; set; }

        Vector2 vel;
        float velGround;

        protected virtual void Awake()
        {
            Motor = GetComponentInParent<MotorBase>();
            Animator = GetComponentInChildren<PlayableAnimationController>();
            GroundableCache = Motor as IGroundable;

            if (Animator.Layers.Count < 0)
                Animator.AddLayer(new PlayableAnimationLayer());
        }

        protected virtual void Update()
        {
            if (Motor != null)
            {
                Vector3 localDir = Quaternion.Euler(RotationOffset) * Quaternion.Inverse(transform.rotation) * Motor.WishMoveDirection;
                TargetDirection = new Vector2(localDir.x, localDir.z).CircleToSquare() * GetStateMultiplier(Motor.State);
            }

            CurrentDirection = (CurrentDirection - TargetDirection).magnitude <= 0.0001f
                ? TargetDirection
                : Vector2.SmoothDamp(CurrentDirection, TargetDirection, ref vel, SmoothTime);

            CurrentGrounded = Mathf.SmoothDamp(CurrentGrounded, GroundableCache.IsGrounded ? 1f : 0f, ref velGround, SmoothTime);

            Animator.Layers[Layer].Play(State);
            State.SetBlendFloat(MoveX, CurrentDirection.x);
            State.SetBlendFloat(MoveY, CurrentDirection.y);
            State.SetBlendFloat(Grounded, CurrentGrounded);
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
