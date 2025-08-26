using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs.Demo
{
    public class FreecamMotor : PlayerMotorBase<Transform>
    {
        public float MinSpeed = 0.5f;
        public float MaxSpeed = 50f;

        public float CurrentSpeed { get; private set; } = 5f;

        public override Vector2 GetInputLook() => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        public override Vector2 GetInputMove() => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        public float GetVerticalMove() => 
            (Input.GetKey(KeyCode.Space) ? 1f : 0f)
            - (Input.GetKey(KeyCode.LeftControl) ? 1f : 0f);

        Vector2 currentLook;

        SingleSetting<float> sensitivity;

        protected override void Awake()
        {
            base.Awake();

            CursorManager.DefaultUnlocked = false;

            CurrentSpeed = 5f;
            SettingsManager.Current.TrySetting("Sensitivity", out sensitivity);
        }

        protected override void Update()
        {
            if (InputBlocker.Blocked)
                return;

            Vector2 vector2 = GetInputLook();
            Vector2 look = currentLook + new Vector2(-vector2.y, vector2.x) * sensitivity.Value;
            look.x = Mathf.Clamp(look.x, -90f, 90f);
            currentLook = look;
            WishLookRotation = Quaternion.Euler(currentLook);

            base.Update();

            CurrentSpeed = Mathf.Clamp(CurrentSpeed + Input.mouseScrollDelta.y, MinSpeed, MaxSpeed);
        }

        protected override void FixedUpdate()
        {
            if (InputBlocker.Blocked)
                return;

            Vector2 vector2 = GetInputMove();
            WishMoveDirection = transform.rotation * new Vector3(vector2.x, GetVerticalMove(), vector2.y).normalized;
            base.FixedUpdate();
        }

        public override void Look(Quaternion rotation) => Component.rotation = rotation;

        public override void Move(Vector3 direction) => Component.position += direction.normalized * (Time.fixedUnscaledDeltaTime * CurrentSpeed * (Input.GetKey(KeyCode.LeftShift) ? 2f : 1f));
    }
}
