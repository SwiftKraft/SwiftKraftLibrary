using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Saving.Settings;
using SwiftKraft.Saving.Settings.UI;
using System.Collections;
using System.Collections.Generic;
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
        currentLook += new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X")) * sensitivity.Value;
        Motor.WishLookRotation = Quaternion.Euler(currentLook);
    }

    private void FixedUpdate()
    {
        Motor.WishMoveDirection = transform.rotation * new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
    }
}
