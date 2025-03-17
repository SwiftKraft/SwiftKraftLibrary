using System;
using UnityEngine;

namespace Player.Movement
{
    [CreateAssetMenu(menuName = "Movement State/Ground")]
    public class PlayerMovementGround : PlayerMovementBasic
    {
        public static event Action onJump;

        public float StepHeight = 0.6f;

        public PlayerMovementAir AirState;
        public PlayerMovementStateBase SlideState;
        public PlayerMovementCrouch CrouchState;
        public PlayerMovementDash Dash;
        public override void StateStarted(PlayerMotor parent)
        {
            if (!parent.IsGrounded)
            {
                parent.CurrentState = AirState;
                return;
            }

           
            BaseStateStarted(parent);
        }

        protected void BaseStateStarted(PlayerMotor parent) => base.StateStarted(parent);

        public override void GroundedChanged(PlayerMotor parent, bool value, bool prev)
        {
            base.GroundedChanged(parent, value, prev);

            if (!value)
            {
                parent.CurrentState = AirState;
                AirState.ReceiveData(JumpSpeed);
            }
        }

        public override void TickUpdate(PlayerMotor parent)
        {
            BaseTickUpdate(parent);

            if (parent.GetWishDir() != Vector3.zero)
            {
                float climb = parent.GroundPoint.position.StepClimb(StepHeight, 0.1f, parent.Collider.radius, parent.GroundLayers);
                if (climb > 0)
                    parent.transform.position += climb * Vector3.up;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyUp(KeyCode.LeftShift))
                {
                    
                    return;
                }
                else
                {
                    parent.CurrentState = SlideState;
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                parent.CurrentState = Dash;
            }

            

        }

        public override void TryJump(PlayerMotor parent, float speed = -1f)
        {
            base.TryJump(parent);
            parent.PlayMotorSound(1);
            parent.CurrentState = AirState;
            onJump?.Invoke();
        }

        protected void BaseTickUpdate(PlayerMotor parent) => base.TickUpdate(parent);
    }
}
