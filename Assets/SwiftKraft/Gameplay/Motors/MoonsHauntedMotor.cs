using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public class MoonsHauntedMotor : CharacterControllerMotor
    {
        public float SprintSpeed = 7f;

        public Vector3 RequestedVelocity { get; set; }
        [field: SerializeField]
        public float RequestedVelocityDecay { get; set; } = 10f;

        public override float CurrentSpeed => WishSprint ? SprintSpeed : MoveSpeed;
        public override float MoveFactorRate => CurrentSpeed;
        public override float RawMoveFactorRate => CurrentSpeed / MoveSpeed;

        public bool WishSprint { get; set; }

        protected override void FixedUpdate()
        {
            RequestedVelocity = Vector3.MoveTowards(RequestedVelocity, Vector3.zero, RequestedVelocityDecay * Time.fixedDeltaTime);

            base.FixedUpdate();

            State += WishSprint ? 1 : 0;
            Debug.DrawRay(LookPoint.position, LookPoint.forward, Color.blue, Time.fixedDeltaTime);
        }

        public override void Move(Vector3 direction)
        {
            base.Move(direction);
            Component.Move(RequestedVelocity);
        }
    }
}
