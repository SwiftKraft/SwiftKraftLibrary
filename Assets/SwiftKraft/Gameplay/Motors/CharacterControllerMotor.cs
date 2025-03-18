using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerMotor : MotorBase<CharacterController>
    {
        public float MoveSpeed = 5f;
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

        float currentGravity;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!Component.isGrounded)
                currentGravity -= Gravity * Time.fixedDeltaTime;
            else
                currentGravity = 0;

            Component.Move(Vector3.down * (currentGravity * Time.fixedDeltaTime));
        }

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
