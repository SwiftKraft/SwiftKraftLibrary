using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Motors
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSPlayerCharacterControllerMotor : PlayerMotorBase<CharacterController>
    {
        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }

        public Transform GroundPoint;

        public LayerMask GroundLayers;

        public float GroundRadius = 0.1f;
        public float JumpSpeed = 5f;
        public float MoveSpeed = 5f;
        public float SprintSpeed = 8f;
        public float Gravity = 9.81f;

        readonly Timer coyoteTime = new(0.2f, false);
        readonly Trigger jumpInput = new();
        float currentGravity;

        public float Height
        {
            get => Component.height;
            set
            {
                Component.height = value;
                Component.center = Vector3.up * (value / 2f);
            }
        }

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

            SettingsManager.Current.TrySetting("Sensitivity", out SingleSetting<float> setting);
            Vector2 inputLook = GetInputLook() * (setting == null ? 1f : setting.Value);
            Vector3 wishLookEulers = WishLookRotation.eulerAngles;

            WishLookRotation = Quaternion.Euler(Mathf.Clamp(wishLookEulers.x.NormalizeAngle() + inputLook.y, -90f, 90f), wishLookEulers.y + inputLook.x, wishLookEulers.z);
        }

        protected override void FixedUpdate()
        {
            IsGrounded = Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (!IsGrounded)
            {
                currentGravity -= Gravity * Time.fixedDeltaTime;
                coyoteTime.Tick(Time.fixedDeltaTime);
            }
            else
            {
                currentGravity = -1f;
                coyoteTime.Reset();
            }

            if (jumpInput.GetTrigger() && !coyoteTime.Ended)
            {
                currentGravity = JumpSpeed;
                IsGrounded = false;
                coyoteTime.Tick(coyoteTime.MaxValue);
            }

            Component.Move(Vector3.up * (currentGravity * Time.fixedDeltaTime));

            base.FixedUpdate();

            if (!Enabled)
            {
                IsSprinting = false;
                State = 0;
                return;
            }

            Vector2 inputMove = GetInputMove();

            IsSprinting = Input.GetKey(KeyCode.LeftShift) && inputMove.y > 0f;

            State = inputMove.sqrMagnitude > 0f && IsGrounded ? 1 + (IsSprinting && IsGrounded ? 1 : 0) : 0;

            WishMoveDirection = transform.rotation * new Vector3(inputMove.x, 0f, inputMove.y).normalized;
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
            Vector3 vel = direction * (Time.fixedDeltaTime * (IsSprinting ? SprintSpeed : MoveSpeed));
            Component.Move(vel);
        }
    }
}
