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

        public float JumpVelocity = 5f;
        public float MaxVelocity = 6f;
        public float Acceleration = 30f;
        public float AirAcceleration = 2f;
        public float GroundDrag = 4f;
        public float AirDrag = 0f;

        readonly Trigger jumpInput = new();

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

            if (Input.GetKeyDown(KeyCode.Space))
                jumpInput.SetTrigger();

            Vector2 inputLook = GetInputLook();
            Vector3 wishLookEulers = WishLookRotation.eulerAngles;

            WishLookRotation = Quaternion.Euler(Mathf.Clamp(wishLookEulers.x.NormalizeAngle() + inputLook.y, -90f, 90f), wishLookEulers.y + inputLook.x, wishLookEulers.z);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            IsGrounded = Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (jumpInput.GetTrigger() && IsGrounded)
            {
                Vector3 temp = Component.velocity;
                temp.y = JumpVelocity;
                Component.velocity = temp;
                IsGrounded = false;
            }

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
            Vector3 horizontalVelocity = Component.velocity;
            horizontalVelocity.y = 0f;

            // float maxVelocityFactor = Mathf.InverseLerp(MaxVelocity * MaxVelocity, 0f, horizontalVelocity.sqrMagnitude);
            float perpendicularity = 1f - Mathf.Abs(Vector3.Dot(horizontalVelocity.normalized, direction.normalized));
            float acceleration = IsGrounded ? Acceleration : AirAcceleration;

            Component.velocity += direction
                * (acceleration
                * (MaxVelocity * MaxVelocity <= horizontalVelocity.sqrMagnitude ? perpendicularity : 1f)
                * Time.fixedDeltaTime);
        }
    }
}
