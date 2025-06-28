using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.NetworkedFPS.Motors
{
    [RequireComponent(typeof(Rigidbody))]
    public class MultiplayerFPSControllerMotor : PlayerMotorBase<Rigidbody>, IGroundable
    {
        public Transform GroundPoint;
        public float GroundRadius;
        public LayerMask GroundLayers;

        public ModifiableStatistic Acceleration = new(10f);
        public ModifiableStatistic AirAcceleration = new(2f);
        public ModifiableStatistic MaxSpeed = new(5f);
        public ModifiableStatistic JumpSpeed = new(5f);

        public float ReferenceFOV = 90f;

        SingleSetting<float> sensitivity;
        SingleSetting<float> aimSensitivity;

        Camera _mainCamera;

        readonly Trigger jumpTrigger = new();

        public bool IsGrounded { get; set; }

        public override Vector2 GetInputLook() => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        public override Vector2 GetInputMove() => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        protected override void Awake()
        {
            base.Awake();

            _mainCamera = Camera.main;

            SettingsManager.Current.TrySetting("Sensitivity", out sensitivity);
            SettingsManager.Current.TrySetting("AimSensitivity", out aimSensitivity);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void Update()
        {
            base.Update();

            float fovSensMult = _mainCamera.fieldOfView / ReferenceFOV;

            Vector2 inputLook = (sensitivity == null ? 1f : sensitivity.Value) * Mathf.LerpUnclamped(1f, fovSensMult, aimSensitivity != null ? aimSensitivity.Value : 1f) * GetInputLook();
            Vector3 wishLookEulers = WishLookRotation.eulerAngles;

            WishLookRotation = Quaternion.Euler(Mathf.Clamp(wishLookEulers.x.NormalizeAngle() + inputLook.y, -90f, 90f), wishLookEulers.y + inputLook.x, wishLookEulers.z);

            if (Input.GetKeyDown(KeyCode.Space))
                jumpTrigger.SetTrigger();
        }

        protected override void FixedUpdate()
        {
            IsGrounded = Physics.CheckSphere(GroundPoint.position, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            Vector2 inputMove = GetInputMove().normalized;
            WishMoveDirection = transform.rotation * new Vector3(inputMove.x, 0f, inputMove.y);

            if (jumpTrigger.GetTrigger() && IsGrounded)
                Component.velocity += Vector3.up * JumpSpeed;

            base.FixedUpdate();
        }

        public override void Look(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            Component.rotation = Quaternion.Euler(0f, euler.y, 0f);
            LookPoint.localRotation = Quaternion.Euler(-euler.x, 0f, 0f);
        }

        public override void Move(Vector3 direction)
        {
            if (!IsGrounded && direction == Vector3.zero)
                return;

            Vector3 horiz = Component.velocity;
            horiz.y = 0f;
            horiz = Vector3.MoveTowards(horiz, direction * MaxSpeed, (IsGrounded ? Acceleration : AirAcceleration) * Time.fixedDeltaTime);
            Component.velocity = horiz + (Vector3.up * Component.velocity.y);
        }
    }
}
