using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaserAI : MonoBehaviour
{
    public Transform target;          // The target (e.g., Player)
    public float moveSpeed = 5f;      // Movement speed
    public float stoppingDistance = 1f; // Distance to stop near the player

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // Ensure gravity is enabled
    }

    void FixedUpdate()
    {
        if (target == null) return;

        // Get direction toward the target
        Vector3 directionToTarget = (target.position - transform.position);
        directionToTarget.y = 0; // Keep movement on the ground

        // Stop moving if within stopping distance
        if (directionToTarget.magnitude <= stoppingDistance)
        {
            rb.velocity = Vector3.zero; // Stop moving
            return;
        }

        // Move toward the target
        rb.velocity = directionToTarget.normalized * moveSpeed + new Vector3(0, rb.velocity.y, 0);

        // Face the target
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * moveSpeed);
        }
    }
}