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

        protected override void Awake()
        {
            base.Awake();
            Component.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        protected override void FixedUpdate()
        {
            IsGrounded = Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            State = WishMoveDirection.sqrMagnitude > 0 ? 1 : 0;

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
            Vector3 vel = Component.velocity;
            vel.x = direction.x * MoveSpeed;
            vel.z = direction.z * MoveSpeed;
            Component.velocity = vel;
        }
    }
}
