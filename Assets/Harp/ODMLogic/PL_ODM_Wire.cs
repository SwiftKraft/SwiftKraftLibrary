using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PL_ODM_Wire : MonoBehaviour
{
    [Header("References")]
    public PL_ODM playerODMGear;
    PL_ODM_Wire_Spring spring;
    public int hookIndex;

    [Header("Line Renderer Animation")]
    public float velocity = 15f;
    public float strength = 800f;
    public float waveHeight = 2f;
    public float damper = 14f;
    public int waveCount = 3;
    public int quality = 100;
    public AnimationCurve effectCurve;

    private void Awake()
    {
        spring = new PL_ODM_Wire_Spring();
        spring.SetTarget(0);
    }

    private void FixedUpdate()
    {
        DrawODMLineAnmiated();
    }


    void DrawODMLine()
    {
        if (!playerODMGear) return;

        if (playerODMGear.hookJoints[hookIndex])
        {
            Vector3 direction = (playerODMGear.hookSwingPoints[hookIndex] - playerODMGear.hookPositions[hookIndex]).normalized;
            float distance = playerODMGear.hookEjectForce * Time.deltaTime;
            playerODMGear.hookPositions[hookIndex] += direction * distance;

            playerODMGear.hookWireRenderers[hookIndex].positionCount = 2;
            playerODMGear.hookWireRenderers[hookIndex].SetPosition(0, playerODMGear.hookStartTransforms[hookIndex].position);
            playerODMGear.hookWireRenderers[hookIndex].SetPosition(1, playerODMGear.hookPositions[hookIndex]);
        }
        else
        {
            if (Vector3.Distance(playerODMGear.hookPositions[hookIndex], playerODMGear.hookStartTransforms[hookIndex].position) > 2f)
            {
                Vector3 direction = (playerODMGear.hookStartTransforms[hookIndex].position - playerODMGear.hookPositions[hookIndex]).normalized;
                float distance = playerODMGear.hookEjectForce * Time.deltaTime;
                playerODMGear.hookPositions[hookIndex] += direction * distance;

                playerODMGear.hookWireRenderers[hookIndex].positionCount = 2;
                playerODMGear.hookWireRenderers[hookIndex].SetPosition(0, playerODMGear.hookStartTransforms[hookIndex].position);
                playerODMGear.hookWireRenderers[hookIndex].SetPosition(1, playerODMGear.hookPositions[hookIndex]);

                playerODMGear.reelingInOutState[hookIndex] = 2;
            }
            else
            {
                playerODMGear.hookPositions[hookIndex] = playerODMGear.hookStartTransforms[hookIndex].position;
                playerODMGear.hookWireRenderers[hookIndex].positionCount = 0;
                playerODMGear.hooksReady[hookIndex] = true;
                playerODMGear.reelingInOutState[hookIndex] = 3;
                playerODMGear.hookPositions[hookIndex] = playerODMGear.hookStartTransforms[hookIndex].position;
            }
        }
    }

    void DrawODMLineAnmiated()
    {
        if (!playerODMGear) return;

        if (playerODMGear.hookJoints[hookIndex] == null || playerODMGear.reelingInOutState[hookIndex] == 3)
        {
            if (Vector3.Distance(playerODMGear.hookPositions[hookIndex], playerODMGear.hookStartTransforms[hookIndex].position) < 2f)
            {
                playerODMGear.hookPositions[hookIndex] = playerODMGear.hookStartTransforms[hookIndex].position;
                playerODMGear.hookWireRenderers[hookIndex].positionCount = 0;
                playerODMGear.hooksReady[hookIndex] = true;
                playerODMGear.reelingInOutState[hookIndex] = 3;
                playerODMGear.hookPositions[hookIndex] = playerODMGear.hookStartTransforms[hookIndex].position;
                return;
            }

            Vector3 direction = (playerODMGear.hookStartTransforms[hookIndex].position - playerODMGear.hookPositions[hookIndex]).normalized;
            float distance = playerODMGear.hookEjectForce * Time.deltaTime;
            playerODMGear.hookPositions[hookIndex] += direction * distance;

            playerODMGear.hookWireRenderers[hookIndex].positionCount = 2;
            playerODMGear.hookWireRenderers[hookIndex].SetPosition(0, playerODMGear.hookStartTransforms[hookIndex].position);
            playerODMGear.hookWireRenderers[hookIndex].SetPosition(1, playerODMGear.hookPositions[hookIndex]);

            playerODMGear.reelingInOutState[hookIndex] = 2;

            
        }
        else if (playerODMGear.hookJoints[hookIndex] && playerODMGear.reelingInOutState[hookIndex] != 3 && playerODMGear.reelingInOutState[hookIndex] != 0)
        {
            float speedForLerp = playerODMGear.hookEjectForce * Time.deltaTime;

            if (playerODMGear.hookWireRenderers[hookIndex].positionCount <= 2)
            {
                spring.SetVelocity(velocity);
                playerODMGear.hookWireRenderers[hookIndex].positionCount = quality + 1;
            }

            spring.SetDamper(damper);
            spring.SetStrength(strength);
            spring.Update();

            Vector3 up = Quaternion.LookRotation((playerODMGear.hookSwingPoints[hookIndex] - playerODMGear.hookStartTransforms[hookIndex].position).normalized) * Vector3.up * UnityEngine.Random.Range(-1, 1);
            Vector3 right = Quaternion.LookRotation((playerODMGear.hookSwingPoints[hookIndex] - playerODMGear.hookStartTransforms[hookIndex].position).normalized) * Vector3.right * UnityEngine.Random.Range(-1, 1);

            playerODMGear.hookPositions[hookIndex] = Vector3.Lerp(playerODMGear.hookPositions[hookIndex], playerODMGear.hookSwingPoints[hookIndex], speedForLerp);

            for (int i = 0; i < quality + 1; i++)
            {
                float delta = i / (float)quality;
                Vector3 offset = (up * waveHeight * MathF.Sin(delta * waveHeight * Mathf.PI) * spring.Value * effectCurve.Evaluate(delta)) + ((right * waveHeight * MathF.Sin(delta * waveHeight * Mathf.PI) * spring.Value * effectCurve.Evaluate(delta)));

                playerODMGear.hookWireRenderers[hookIndex].SetPosition(i, Vector3.Lerp(playerODMGear.hookStartTransforms[hookIndex].position, playerODMGear.hookPositions[hookIndex], delta) + offset);
            }
        }
    }
}
