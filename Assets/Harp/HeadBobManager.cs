using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadBobManager : MonoBehaviour
{ public float bobFrequency = 2.0f;
    public float bobFrequencySprint;
    public float bobFrequencyWalk = 15f;
    public float bobAmplitude = 0.1f;
    public float returnSpeed = 5.0f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float timer = 0.0f;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            bobFrequency = bobFrequencySprint;
        }
        else
        {
            bobFrequency = bobFrequencyWalk;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        float moveMagnitude = new Vector2(moveX, moveZ).magnitude;

        if (moveMagnitude > 0)
        {
            timer += Time.deltaTime * bobFrequency;
            float newY = Mathf.Sin(timer) * bobAmplitude;
            transform.localPosition = new Vector3(originalPosition.x, originalPosition.y + newY, originalPosition.z);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * returnSpeed);
        }

        Quaternion newRotation = Quaternion.Euler(originalRotation.eulerAngles.x, originalRotation.eulerAngles.y + Mathf.Sin(timer) * 2f, originalRotation.eulerAngles.z);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, newRotation, Time.deltaTime * returnSpeed);
    }
}
