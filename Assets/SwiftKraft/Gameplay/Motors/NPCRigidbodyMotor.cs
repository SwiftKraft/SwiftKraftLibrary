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

        public override Quaternion LookInterpolation() => Quaternion.RotateTowards(CurrentLookRotation, WishLookRotation, (LookInUpdate ? Time.deltaTime : Time.fixedDeltaTime) * TurnSpeed);

        public override void Look(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            Component.rotation = Quaternion.Euler(0f, euler.y, 0f);
            LookPoint.localRotation = Quaternion.Euler(euler.x, 0f, 0f);
        }

        public override void Move(Vector3 direction)
        {
            direction *= MoveSpeed;
            Vector3 vel = Component.velocity;
            vel.x = direction.x;
            vel.z = direction.z;
            Component.velocity = vel;
        }
    }
}
