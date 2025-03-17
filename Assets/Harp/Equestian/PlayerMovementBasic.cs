using UnityEditor;
using UnityEngine;

namespace Player.Movement
{
    public abstract class PlayerMovementBasic : PlayerMovementStateBase
    {
        public float CameraHeight = 1.8f;
        public float BodyHeight = 2f;
        public float Gravity = -30f;
        public float ControlThreshold = 10f;
        public float Speed = 0.8f;
        public float GasSpeed = 0.8f;
        public float WalkSpeed = 0.4f;
        public float Drag = 14f;
        public float JumpSpeed = 18f;
        public float JumpBuffer = 0.1f;

        protected float CurrentJumpBuffer;

        float prevDrag;

        public override void StateStarted(PlayerMotor parent)
        {
            CurrentJumpBuffer = 0f;
            prevDrag = parent.Rigidbody.drag;
            parent.Rigidbody.drag = Drag;
            parent.TargetCameraHeight = CameraHeight;
            parent.Height = BodyHeight;
        }

        public override void StateEnded(PlayerMotor parent)
        {
            parent.Rigidbody.drag = prevDrag;
        }

        public override void InputUpdate(PlayerMotor parent)
        {
            if (parent.GetWishJump())
                CurrentJumpBuffer = JumpBuffer;
        }

        public override void TickUpdate(PlayerMotor parent)
        {
            parent.Rigidbody.AddForce(Vector3.up * Gravity);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                Movement(parent, GasSpeed, ControlThreshold);
            }
            else
            {
                Movement(parent, Speed, ControlThreshold);
            }
            

            if (CurrentJumpBuffer > 0f)
            {
                TryJump(parent);
                CurrentJumpBuffer -= Time.fixedDeltaTime;
            }
            else if (CurrentJumpBuffer < 0f)
                CurrentJumpBuffer = 0f;
        }

        public virtual void TryJump(PlayerMotor parent, float speed = -1f)
        {
            parent.RecentJumpTimer.Reset();
            CurrentJumpBuffer = 0f;
            SetGravity(parent, speed < 0f ? JumpSpeed : speed);
        }

        public static void Movement(PlayerMotor parent, float speed, float controlThreshold)
        {
            Vector3 horizontalVelocity = new(parent.Rigidbody.velocity.x, 0f, parent.Rigidbody.velocity.z);
            float angle = Vector3.Angle(horizontalVelocity.normalized, parent.GetWishDir());
            if (parent.IsGrounded)
                parent.Rigidbody.AddForce(speed * (Quaternion.FromToRotation(Vector3.up, parent.GetGroundNormal()) * parent.GetWishDir()), ForceMode.Impulse);
            else
            {
                if (controlThreshold > horizontalVelocity.magnitude)
                    parent.Rigidbody.AddForce(speed * parent.GetWishDir(), ForceMode.Impulse);
                else
                    parent.Rigidbody.AddForce(Mathf.Lerp(0f, speed, Mathf.InverseLerp(0f, 90f, angle)) * parent.GetWishDir(), ForceMode.Impulse);
            }
        }

        public static void SetGravity(PlayerMotor parent, float gravity)
        {
            Vector3 v = parent.Rigidbody.velocity;
            v.y = gravity;
            parent.Rigidbody.velocity = v;
        }
    }
}
