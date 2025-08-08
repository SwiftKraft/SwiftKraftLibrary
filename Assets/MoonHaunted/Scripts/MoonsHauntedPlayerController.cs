using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Utils;
using UnityEngine;

[RequireComponent(typeof(MoonsHauntedMotor))]
public class MoonsHauntedPlayerController : MonoBehaviour
{
    public MoonsHauntedMotor Motor { get; private set; }

    Vector2 currentLook;

    SingleSetting<float> sensitivity;

    private void Awake()
    {
        Motor = GetComponent<MoonsHauntedMotor>();
        SettingsManager.Current.TrySetting("Sensitivity", out sensitivity);
    }

    private void Update()
    {
        Vector2 look = currentLook;
        look += new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X")) * sensitivity.Value;
        look.x = Mathf.Clamp(look.x, -90f, 90f);
        currentLook = look;
        Motor.WishLookRotation = Quaternion.Euler(currentLook);
    }

    private void FixedUpdate() => Motor.WishMoveDirection = transform.rotation * new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

    public void OverrideLook()
    {
        if (Motor != null) {
            Vector3 euler = Motor.CurrentLookRotation.eulerAngles;
            euler.x = euler.x.NormalizeAngle();
            currentLook = euler;
        }
    }
}
