using UnityEngine;

namespace Player.Movement
{
    [CreateAssetMenu(menuName = "Movement State/Crouch")]
    public class PlayerMovementCrouch : PlayerMovementGround
    {
        public PlayerMovementBasic ExitState;

        public override void StateStarted(PlayerMotor parent)
        {
            if (!parent.IsGrounded)
            {
                parent.CurrentState = AirState;
                return;
            }

            BaseStateStarted(parent);
        }

        public override void TickUpdate(PlayerMotor parent)
        {
            BaseTickUpdate(parent);

            if (!Input.GetKey(KeyCode.LeftControl) && parent.ViableHeight > ExitState.BodyHeight)
            {
                parent.CurrentState = ExitState;
                return;
            }
        }
    }
}