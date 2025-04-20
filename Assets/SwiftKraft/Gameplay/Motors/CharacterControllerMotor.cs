using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerMotor : MotorBase<CharacterController>, IGroundable
    {
        public float MoveSpeed = 5f;
        public float TurnSpeed = 480f;
        public float Gravity = 9.81f;

        public float Height
        {
            get => Component.height;
            set
            {
                Component.height = value;
                Component.center = Vector3.up * (value / 2f);
            }
        }

        public bool IsGrounded => Component.isGrounded;

        float currentGravity;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!IsGrounded)
                currentGravity -= Gravity * Time.fixedDeltaTime;
            else
                currentGravity = 0;

            Component.Move(Vector3.up * (currentGravity * Time.fixedDeltaTime));
        }

        public override Quaternion LookInterpolation() => Quaternion.RotateTowards(CurrentLookRotation, WishLookRotation, Time.fixedDeltaTime * TurnSpeed);

        public override void Look(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(0f, euler.y, 0f);
            LookPoint.localRotation = Quaternion.Euler(-euler.x, 0f, 0f);
        }

        public override void Move(Vector3 direction)
        {
            Vector3 vel = direction * (Time.fixedDeltaTime * MoveSpeed);
            Component.Move(vel);
        }
    }
}
