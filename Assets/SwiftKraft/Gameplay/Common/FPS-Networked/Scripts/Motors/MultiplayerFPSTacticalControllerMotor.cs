using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.NetworkedFPS.Motors
{
    [RequireComponent(typeof(Rigidbody))]
    public class MultiplayerFPSTacticalControllerMotor : PlayerMotorBase<Rigidbody>
    {
        public ModifiableStatistic Acceleration = new(10f);
        public ModifiableStatistic MaxSpeed = new(5f);

        public float GroundDrag = 12f;

        public float ReferenceFOV = 90f;

        SingleSetting<float> sensitivity;
        SingleSetting<float> aimSensitivity;

        Camera _mainCamera;

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
        }

        protected override void FixedUpdate()
        {
            Vector2 inputMove = GetInputMove();
            WishMoveDirection = transform.rotation * new Vector3(inputMove.x, 0f, inputMove.y);

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
            Component.velocity += direction
                * (Acceleration
                * Time.fixedDeltaTime);
        }
    }
}
