using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Motors
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSPlayerCharacterControllerMotor : PlayerMotorBase<CharacterController>
    {
        public float CeilingHeight => Physics.Raycast(GroundPoint.position, Vector3.up, out RaycastHit hit, Mathf.Infinity, GroundLayers, QueryTriggerInteraction.Ignore) ? hit.distance : Mathf.Infinity;

        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }

        public Transform GroundPoint;

        public LayerMask GroundLayers;

        public float GroundRadius = 0.1f;
        public float JumpSpeed = 5f;
        public float MoveSpeed = 5f;
        public float CrouchSpeed = 3f;
        public float SprintSpeed = 8f;
        public float Gravity = 9.81f;
        public float CrouchHeight = 1f;
        public float CrouchCameraHeight = 0.8f;

        public Camera MainCamera;
        public float ReferenceFOV;

        readonly Timer coyoteTime = new(0.2f, false);
        readonly Trigger jumpInput = new();
        float currentGravity;

        public SmoothDampInterpolater CrouchInterp;

        public bool WishCrouch { get; private set; }

        public float Height
        {
            get => Component.height;
            set
            {
                Component.height = value;
                Component.center = Vector3.up * (value / 2f);
            }
        }

        SingleSetting<float> sensitivity;
        SingleSetting<float> aimSensitivity;

        float originalHeight;
        float originalCameraHeight;

        protected override void Awake()
        {
            base.Awake();

            originalCameraHeight = LookPointHeight;
            originalHeight = Height;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SettingsManager.Current.TrySetting("Sensitivity", out sensitivity);
            SettingsManager.Current.TrySetting("AimSensitivity", out aimSensitivity);
        }

        protected override void Update()
        {
            base.Update();

            if (!Enabled || InputBlocker.Blocked)
                return;

            if (Input.GetKeyDown(KeyCode.Space))
                jumpInput.SetTrigger();

            float fovSensMult = MainCamera.fieldOfView / ReferenceFOV;

            Vector2 inputLook = (sensitivity == null ? 1f : sensitivity.Value) * Mathf.LerpUnclamped(1f, fovSensMult, aimSensitivity != null ? aimSensitivity.Value : 1f) * GetInputLook();
            Vector3 wishLookEulers = WishLookRotation.eulerAngles;

            WishLookRotation = Quaternion.Euler(Mathf.Clamp(wishLookEulers.x.NormalizeAngle() + inputLook.y, -90f, 90f), wishLookEulers.y + inputLook.x, wishLookEulers.z);
        }

        protected override void FixedUpdate()
        {
            IsGrounded = Vehicle == null && Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            CrouchInterp.Tick(Time.fixedDeltaTime);
            CrouchInterp.MaxValue = WishCrouch ? 1f : 0f;

            Height = Mathf.Lerp(originalHeight, CrouchHeight, CrouchInterp.CurrentValue);
            LookPointHeight = Mathf.Lerp(originalCameraHeight, CrouchCameraHeight, CrouchInterp.CurrentValue);

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

            if (Vehicle == null)
                Component.Move(Vector3.up * (currentGravity * Time.fixedDeltaTime));
            else
                currentGravity = 0f;

            base.FixedUpdate();

            if (!Enabled || InputBlocker.Blocked)
            {
                IsSprinting = false;
                State = 0;
                return;
            }

            Vector2 inputMove = GetInputMove();

            WishCrouch = Input.GetKey(KeyCode.LeftControl) || CeilingHeight < originalHeight;
            IsSprinting = !WishCrouch && Input.GetKey(KeyCode.LeftShift) && inputMove.y > 0f;

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
            Vector3 vel = direction * (Time.fixedDeltaTime * (IsSprinting ? SprintSpeed : (WishCrouch ? CrouchSpeed : MoveSpeed)));
            Component.Move(vel);
        }
    }
}
