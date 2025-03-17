using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PL_ODM_Wire_Spring : MonoBehaviour
{
    private float strength;
    private float damper;
    private float target;
    private float velocity;
    private float value;

    public void Update()
    {
        var direction = target - value >= 0 ? 1f : -1f;
        var force = Mathf.Abs(target - value) * strength;
        velocity += (force * direction - velocity * damper) * Time.deltaTime;
        value += velocity * Time.deltaTime;
    }

    public void Reset()
    {
        velocity = 0f;
        value = 0f;
    }

    public void SetValue(float value)
    {
        this.value = value;
    }

    public void SetTarget(float target)
    {
        this.target = target;
    }

    public void SetDamper(float damper)
    {
        this.damper = damper;
    }

    public void SetStrength(float strength)
    {
        this.strength = strength;
    }

    public void SetVelocity(float velocity)
    {
        this.velocity = velocity;
    }

    public float Value => value;
}
