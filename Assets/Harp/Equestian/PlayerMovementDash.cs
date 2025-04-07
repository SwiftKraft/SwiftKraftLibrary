using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

namespace Player.Movement
{
    [CreateAssetMenu(menuName = "Movement State/Dash")]
    public class PlayerMovementDash : PlayerMovementBasic
    {
        public const int DashLayer = 21;
        public const int PlayerLayer = 3;

        private PL_ODM ODM;

        public PlayerMovementStateBase ExitState;

        public float JumpCost = 20f;
        public float Duration = 0.3f;
        public float DashEndMultiplier = 0.3f;

        float timer;

        bool dashJumped;
        bool canDashJump;

        Vector3 direction;

        public override void StateStarted(PlayerMotor parent)
        {
            ODM = parent.GetComponent<PL_ODM>();
            if (ODM != null)
            {
                
                Debug.Log("GroundDash ODM found! Starting gas hop");
            }
            else
            {
                Debug.LogWarning("No PL_ODM component found on the player!");
            }

            base.StateStarted(parent);

            direction = parent.GetWishDir();

            if (direction == Vector3.zero)
                direction = parent.transform.forward;

            timer = Duration;

            parent.Collider.gameObject.layer = DashLayer;
        }

        public override void StateEnded(PlayerMotor parent)
        {
            base.StateEnded(parent);

            parent.Collider.gameObject.layer = PlayerLayer;

            if (dashJumped)
                dashJumped = false;
            else
                parent.Rigidbody.velocity *= DashEndMultiplier;
        }

        public override void TickUpdate(PlayerMotor parent)
        {
            canDashJump = timer > 0f;

            if (timer > 0f)
                timer -= Time.fixedDeltaTime;
            else
            {
                timer = 0f;
                parent.CurrentState = ExitState;
                return;
            }

            parent.Rigidbody.velocity = direction * Speed;

            if (CurrentJumpBuffer > 0f)
            {
                TryJump(parent);
                CurrentJumpBuffer -= Time.fixedDeltaTime;
            }
            else if (CurrentJumpBuffer < 0f)
                CurrentJumpBuffer = 0f;
        }

        public override void TryJump(PlayerMotor parent, float speed = -1)
        {
            if (!canDashJump)
                return;

            base.TryJump(parent, speed);

                dashJumped = true;
            

            parent.CurrentState = ExitState;
        }
    }
}
