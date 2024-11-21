using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwayModule : MonoBehaviour
{
    public float tiltAmount = 15f;
    public float smoothSpeed = 5f;
    public Transform cameraTransform;

    private float targetTilt = 0f;
    private float currentTilt = 0f;

    void FixedUpdate()
    {
        Tilt();
    }

    void Tilt()
    {
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A))
            horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D))
            horizontalInput = 1f;
        targetTilt = horizontalInput * tiltAmount;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * smoothSpeed);
        cameraTransform.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
    }
}

