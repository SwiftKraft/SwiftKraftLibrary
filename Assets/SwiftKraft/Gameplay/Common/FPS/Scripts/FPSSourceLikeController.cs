using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    public class FPSSourceLikeController : PlayerMotorBase<Rigidbody>
    {
        public bool IsGrounded { get; private set; }

        public Transform GroundPoint;
        public LayerMask GroundLayers;
        public float GroundRadius = 0.1f;

        public float MaxVelocity = 6f;
        public float Acceleration = 30f;
        public float AirAcceleration = 2f;
        public float GroundDrag = 4f;
        public float AirDrag = 0f;

        protected virtual void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void Update()
        {
            base.Update();

            if (!Enabled)
                return;

            Vector2 inputLook = GetInputLook();
            Vector3 wishLookEulers = WishLookRotation.eulerAngles;

            WishLookRotation = Quaternion.Euler(Mathf.Clamp(wishLookEulers.x.NormalizeAngle() + inputLook.y, -90f, 90f), wishLookEulers.y + inputLook.x, wishLookEulers.z);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            IsGrounded = Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            Component.drag = IsGrounded ? GroundDrag : AirDrag;

            if (!Enabled)
                return;

            Vector2 inputMove = GetInputMove();
            WishMoveDirection = transform.rotation * new Vector3(inputMove.x, 0f, inputMove.y);
        }

        public override Vector2 GetInputLook() => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        public override Vector2 GetInputMove() => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        public override void Look(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(0f, euler.y, 0f);
            LookPoint.localRotation = Quaternion.Euler(-euler.x, 0f, 0f);
        }

        public override void Move(Vector3 direction)
        {
            if (Component.velocity.sqrMagnitude < MaxVelocity * MaxVelocity)
                Component.velocity += direction * ((IsGrounded ? Acceleration : AirAcceleration) * Time.fixedDeltaTime);
        }
    }
}
