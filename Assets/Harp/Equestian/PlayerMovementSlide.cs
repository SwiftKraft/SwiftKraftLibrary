using UnityEngine;

namespace Player.Movement
{
    [CreateAssetMenu(menuName = "Movement State/Slide")]
    public class PlayerMovementSlide : PlayerMovementBasic
    {
        public const string BoostTag = "SlideBooster";

        public PlayerMovementStateBase ExitState;
        public PlayerMovementCrouch CrouchState;
        public PlayerMovementDash DashState;

        public AnimationCurve SlideSoundVolumeControl;
        public float RequiredVelocity = 3f;
        public float SlideBoostTimer = 2.5f;
        public float SlideBoostMultiplier = 1.5f;
        public float BoosterMultiplier = 1.5f;
        public float BoosterCap = 35f;
        public float BoosterStickSpeed = 2f;
        public float CoyoteTime = 0.3f;

     
        public override void StateStarted(PlayerMotor parent)
        {
            Vector3 horizVel = parent.Rigidbody.velocity;
            horizVel.y = 0f;

            base.StateStarted(parent);

            if ((!Input.GetKey(KeyCode.LeftControl) || !parent.IsGrounded) && !IsOnSlideBooster(parent))
            {
                parent.CurrentState = ExitState;
                return;
            }

            if (horizVel.magnitude <= RequiredVelocity)
            {
                parent.CurrentState = CrouchState;
                return;
            }

            if (parent.slideBoostTimer <= 0f && horizVel.magnitude >= 1f)
            {
                parent.slideBoostTimer = SlideBoostTimer;
                parent.Rigidbody.velocity *= SlideBoostMultiplier;
                parent.PlayMotorParticle(0);
                return;
            }
        }

        public override void StateEnded(PlayerMotor parent)
        {
            if (parent.TryGetSound(3, out AudioSource au) && au.isPlaying)
                au.Stop();

            base.StateEnded(parent);
        }

        public override void TickUpdate(PlayerMotor parent)
        {
            Vector3 horizVel = parent.Rigidbody.velocity;
            horizVel.y = 0f;

            bool isOnSlideBooster = IsOnSlideBooster(parent);

            if ((!Input.GetKey(KeyCode.LeftControl) || !parent.IsGrounded) && !isOnSlideBooster)
            {
                parent.CurrentState = ExitState;
                ExitState.ReceiveData(JumpSpeed);
                return;
            }

            //IF Sliding and Manuvering Gear is enabled AND ODM has gas enabled 
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.LeftAlt))
            {

                parent.CurrentState = DashState;

            }


            if (horizVel.magnitude <= RequiredVelocity && !isOnSlideBooster)
            {
                parent.CurrentState = CrouchState;
                return;
            }

            if (parent.TryGetSound(3, out AudioSource au))
            {
                au.volume = SlideSoundVolumeControl.Evaluate(horizVel.magnitude);
                if (!au.isPlaying)
                    au.Play();
            }

            parent.Rigidbody.drag = isOnSlideBooster ? 0f : Drag;

            if (isOnSlideBooster && horizVel.magnitude < BoosterCap)
            {
                SetGravity(parent, -BoosterStickSpeed);
                parent.Rigidbody.velocity *= BoosterMultiplier;
            }

            base.TickUpdate(parent);
        }

        public override void TryJump(PlayerMotor parent, float speed = -1f)
        {
            base.TryJump(parent);
            parent.PlayMotorSound(1);
            parent.CurrentState = ExitState;
        }

        public bool IsOnSlideBooster(PlayerMotor parent)
        {
            if (!parent.IsGrounded)
                return false;
            return parent.GroundObject.CompareTag(BoostTag);
        }
    }
}
