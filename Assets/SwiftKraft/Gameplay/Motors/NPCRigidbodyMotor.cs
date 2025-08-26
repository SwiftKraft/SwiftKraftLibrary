using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public class NPCRigidbodyMotor : CachePositionMotorBase<Rigidbody>, IGroundable
    {
        public float MoveSpeed = 5f;
        public float TurnSpeed = 480f;
        public float GroundRadius = 0.1f;
        public Transform GroundPoint;
        public LayerMask GroundLayers;

        public bool IsGrounded { get; set; }

        protected override void FixedUpdate()
        {
            IsGrounded = Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            base.FixedUpdate();
        }

        public override void Look(Quaternion rotation)
        {

        }

        public override void Move(Vector3 direction)
        {

        }
    }
}
