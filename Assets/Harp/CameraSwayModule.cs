using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SwiftKraft.Gameplay;
using SwiftKraft.Gameplay.Common.FPS;
using SwiftKraft.Gameplay.Motors;

public class CameraSwayModule : MonoBehaviour
{
    public bool enableCamInput;
    public float rotationSpeed = 100f; // Speed of camera rotation
    public float tiltAngle = 15f; // Maximum tilt angle for the camera
    public float tiltAngleCam;
    public float tiltSpeed = 5f; // Speed at which the tilt transitions

    private Vector3 currentTilt; // Current tilt of the camera

    private Quaternion originalRotation;

    private void Start()
    {
        originalRotation = transform.localRotation;
    }

    void FixedUpdate()
    {
        Tilt();
    }

    void Tilt()
    {

        // Get directional input (WASD or arrow keys)
        float xMovement = Input.GetAxisRaw("Horizontal");
        float zMovement = Input.GetAxisRaw("Vertical");
        float xCamera = Input.GetAxisRaw("Mouse X");
        float yCamera = Input.GetAxisRaw("Mouse Y");

        // Calculate the target tilt based on input
        Vector3 targetTilt = Vector3.zero;
        Vector3 targetRot = Vector3.zero;
        if (xMovement != 0 || zMovement != 0 || xCamera != 0 || yCamera !=0)
        {
            targetTilt = new Vector3(
                -zMovement * tiltAngle, // Tilt forward/backward
                0f,                     // No tilt sideways
                xMovement * tiltAngle  // Tilt left/right

                
            );

            targetRot = new Vector3(
                yCamera * tiltAngleCam,
                -xCamera * tiltAngleCam

                );

            if (enableCamInput)
                targetTilt += targetRot;
        }

        // Smoothly transition to the target tilt or back to original rotation
        if (targetTilt == Vector3.zero)
        {
            // Return to original rotation if no input
            currentTilt = Vector3.Lerp(currentTilt, Vector3.zero, tiltSpeed * Time.deltaTime);
        }
        else
        {
            // Transition to the new tilt direction
            currentTilt = Vector3.Lerp(currentTilt, targetTilt, tiltSpeed * Time.deltaTime);
        }

        // Apply tilt using Quaternions
        Quaternion tiltRotation = Quaternion.Euler(currentTilt);
        transform.localRotation = originalRotation * tiltRotation;
    }
}

