using Player.Movement;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class FOV_By_Speed : MonoBehaviour
{
    [SerializeField]
    private float currentSpeed; // Private variable to store speed
    private PlayerMotor movementScript;

    private float basefov;

    public Camera maincamera;
    [SerializeField]
    private float fovSmoothSpeed = 5f;
    // Start is called before the first frame update
    void Start()
    {
        movementScript = this.GetComponent<PlayerMotor>();
        basefov = maincamera.fieldOfView;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        currentSpeed = Mathf.Ceil(movementScript.Rigidbody.velocity.magnitude);
        UpdateFOV();
    }

    void UpdateFOV()
    {
        float targetFOV = basefov + currentSpeed * 0.5f;
        // Smoothly interpolate between current FOV and target FOV
        maincamera.fieldOfView = Mathf.Lerp(maincamera.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
    }
}
