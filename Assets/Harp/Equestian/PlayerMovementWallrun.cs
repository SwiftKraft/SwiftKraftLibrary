using UnityEngine;

namespace Player.Movement
{
    [CreateAssetMenu(menuName = "Movement State/Wallrun")]
    public class PlayerMovementWallrun : PlayerMovementStateBase
    {
        public const string WallrunnableTag = "Wallrunnable";

        public PlayerMovementStateBase ExitState;

        public AnimationCurve WallrunSoundVolumeControl;

        public float WallrunSpeed;
        public float WallJumpSpeed;
        public float WallJumpVerticalSpeed;
        public float RequiredVelocity = 10f;
        public float AttachSpeed;
        public float AttachDistance;
        public float Drag;
        public float TiltDegrees;
        public float AttachVerticalMultiplier = 0.2f;
        public LayerMask Layer;
        public Vector3 PositionOffset;

        float originalDrag;

        bool rightWall;
        bool jumpNextTick;

        public override void InputUpdate(PlayerMotor parent)
        {
            if (parent.GetWishJump())
                jumpNextTick = true;
        }

        public override void TickUpdate(PlayerMotor parent)
        {
            bool hasWall = GetWall(parent, out RaycastHit hit, out rightWall);

            if (hasWall)
            {
                if (parent.TryGetSound(3, out AudioSource au))
                {
                    au.volume = WallrunSoundVolumeControl.Evaluate(parent.Rigidbody.velocity.magnitude);
                    if (!au.isPlaying)
                        au.Play();
                }

                if (jumpNextTick)
                {
                    parent.Rigidbody.AddForce(hit.normal * WallJumpSpeed, ForceMode.VelocityChange);
                    PlayerMovementBasic.SetGravity(parent, WallJumpVerticalSpeed);
                    jumpNextTick = false;
                    parent.PlayMotorSound(1);
                    parent.CurrentState = ExitState;
                    return;
                }

                Vector3 direction = -Vector3.Cross(hit.normal, Vector3.up);

                int status = rightWall ? 1 : -1;


                parent.Rigidbody.AddForce(direction * (WallrunSpeed * status) + -hit.normal * AttachSpeed, ForceMode.Impulse);
            }
            else
                parent.CurrentState = ExitState;
        }

        public override void StateStarted(PlayerMotor parent)
        {
            originalDrag = parent.Rigidbody.drag;
            parent.Rigidbody.drag = Drag;
            PlayerMovementBasic.SetGravity(parent, parent.Rigidbody.velocity.y * AttachVerticalMultiplier);
        }

        public override void StateEnded(PlayerMotor parent)
        {
            

            parent.Rigidbody.drag = originalDrag;

            if (parent.TryGetSound(3, out AudioSource au) && au.isPlaying)
                au.Stop();
        }

        public bool GetWall(PlayerMotor parent, out RaycastHit hit, out bool right)
        {
            Vector3 velocity = parent.Rigidbody.velocity;
            velocity.y = 0f;

            if (velocity.magnitude < RequiredVelocity)
            {
                hit = new();

                right = false;

                return false;
            }

            if (Physics.Raycast(parent.transform.position + PositionOffset, parent.transform.right, out hit, AttachDistance, Layer, QueryTriggerInteraction.Ignore) && hit.transform.CompareTag(WallrunnableTag))
            {
                right = true;

                return true;
            }
            else if (Physics.Raycast(parent.transform.position + PositionOffset, -parent.transform.right, out hit, AttachDistance, Layer, QueryTriggerInteraction.Ignore) && hit.transform.CompareTag(WallrunnableTag))
            {
                right = false;

                return true;
            }

            right = false;

            return false;
        }
    }
}
