using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    [RequireComponent(typeof(Rigidbody))]
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
        public float CrouchHeight = 1f;
        public float CrouchCameraHeight = 0.8f;
        public float CrouchMaxVelocity = 3f;

        public float CurrentMaxVelocity => WishCrouch && IsGrounded ? CrouchMaxVelocity : MaxVelocity;

        public MoveTowardsInterpolater CrouchInterp;

        float originalHeight;
        float originalCameraHeight;

        public bool WishCrouch { get; private set; }

        public float Height
        {
            get => capsule.height;
            set
            {
                capsule.height = value;
                capsule.center = Vector3.up * (value / 2f);
            }
        }

        public float CameraHeight
        {
            get => LookPoint.localPosition.y;
            set => LookPoint.localPosition = Vector3.up * value;
        }

        readonly Trigger jumpInput = new();

        CapsuleCollider capsule;

        protected virtual void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            capsule = GetComponent<CapsuleCollider>();
            originalCameraHeight = CameraHeight;
            originalHeight = Height;
        }

        protected override void Update()
        {
            base.Update();

            if (!Enabled)
                return;

            WishCrouch = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKeyDown(KeyCode.Space))
                jumpInput.SetTrigger();

            SettingsManager.Current.TrySetting("Sensitivity", out SingleSetting<float> setting);
            Vector2 inputLook = GetInputLook() * (setting == null ? 1f : setting.Value);
            Vector3 wishLookEulers = WishLookRotation.eulerAngles;

            WishLookRotation = Quaternion.Euler(Mathf.Clamp(wishLookEulers.x.NormalizeAngle() + inputLook.y, -90f, 90f), wishLookEulers.y + inputLook.x, wishLookEulers.z);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            IsGrounded = Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            Component.drag = IsGrounded ? GroundDrag : AirDrag;

            CrouchInterp.Tick(Time.fixedDeltaTime);
            CrouchInterp.MaxValue = WishCrouch ? 1f : 0f;

            if (!Enabled)
                return;

            if (jumpInput.GetTrigger() && IsGrounded)
            {
                Vector3 temp = Component.velocity;
                temp.y = JumpVelocity;
                Component.velocity = temp;
                IsGrounded = false;
            }

            Height = Mathf.Lerp(originalHeight, CrouchHeight, CrouchInterp.CurrentValue);
            CameraHeight = Mathf.Lerp(originalCameraHeight, CrouchCameraHeight, CrouchInterp.CurrentValue);

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

            float perpendicularity = 1f - Mathf.Abs(Vector3.Dot(horizontalVelocity.normalized, direction.normalized));
            float acceleration = IsGrounded ? Acceleration : AirAcceleration;

            Component.velocity += direction
                * (acceleration
                * (CurrentMaxVelocity * CurrentMaxVelocity <= horizontalVelocity.sqrMagnitude ? perpendicularity : 1f)
                * Time.fixedDeltaTime);
        }
    }
}
