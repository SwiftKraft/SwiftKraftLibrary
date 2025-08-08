using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public class MoonsHauntedMotor : CharacterControllerMotor
    {
        public float SprintSpeed = 7f;

        public override float CurrentSpeed => WishSprint ? SprintSpeed : MoveSpeed;

        public override float MoveFactorRate => CurrentSpeed;

        public override float RawMoveFactorRate => CurrentSpeed / MoveSpeed;

        public bool WishSprint { get; set; }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
             
            State += WishSprint ? 1 : 0;

            Debug.DrawRay(LookPoint.position, LookPoint.forward, Color.blue, Time.fixedDeltaTime);
        }
    }
}
