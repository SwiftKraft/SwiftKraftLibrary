using Player.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PL_ResourceManagement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PL_ODM _odm;
    [SerializeField] PlayerMotor _mov;
    [SerializeField] PL_GForces _gloc;
    //[SerializeField] PL_DeathRagdoll _deathrgdl;
    [SerializeField] PL_Voice _voice;
    [SerializeField] Behaviour[] behavioursToDisableOnDeath;
    [SerializeField] Image heartImage;
    [SerializeField] AudioSource heartAudioSource;
    [SerializeField] AudioClip[] heartAudioClips;
    [SerializeField] Image[] bodyPartImages;
    [SerializeField] Color healthyColor = new Color(71, 255, 0, 255);
    [SerializeField] Color brokenColor = new Color(255, 0, 0, 255);
    [SerializeField] Color bloodTransparencyColor = new Color(0, 0, 0, 0);

    [Header("Ragdoll")]
    [SerializeField] Rigidbody[] bodyPartsKinematicToggle;
    [SerializeField] Animator ragdollAnimator;

    #region Body status
    [Header("Limbs")]
    [SerializeField] public float[] limbs = new float[] { 100, 100, 100, 100, 100, 100 };

    public enum Limbs
    {
        Head,
        Torso,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg
    };

    [SerializeField] float overallHealth = 100f;

    [Header("States")]
    [SerializeField] bool isDead = false;
    [SerializeField] bool isInjured = false;
    [SerializeField] bool isDowned = false;
    [SerializeField] bool isStraining = false;

    [Header("Prottection")]
    [SerializeField] bool hasHelmet = true;

    [Header("Blood")]
    //[SerializeField] float maxBloodLevel = 4000f;
    [SerializeField] float bloodLevel = 4000f;
    [SerializeField] bool isBleeding = false;
    //[SerializeField] float bleedMultiplier = 1f;

    [Header("Heart")]
    [SerializeField] float heartrate = 80f;
    [SerializeField] float maxStamina = 100f;
    [SerializeField] bool isExhausted = false;
    float currentStamina = 100f;

    private void Update()
    {
        

        if (Input.GetKeyDown(KeyCode.K) && !isDead)
        {
            DamageLimb((int)Limbs.Head, 10000);
        }
    }

    public void Start()
    {
        StartCoroutine(Heartrate());
        SetInitialHealth();
        UnRagdoll();
    }

    public bool ExhaustedCheck()
    {
        return isExhausted;    
    }

    public bool InjuredCheck()
    {
        return isDowned;
    }

    public bool StrainingCheck()
    {
        return isStraining;
    }

    public void SetInitialHealth()
    {
        for (int i = 0; i < limbs.Length; i++)
        {
            limbs[i] = 100f;
        }
        CalculateOverallHealth();
    }

    public float GetCurrentOverallHealth()
    {
        CalculateOverallHealth();
        return overallHealth;
    }

    public float GetCurrentStamina()
    {
        CalculateOverallHealth();
        return currentStamina;
    }

    public void CalculateOverallHealth()
    {
        if (isDead) return;

        float average = 0;
        for (int i = 0; i < limbs.Length; i++)
        {
            average += limbs[i];
        }
        overallHealth = PL_ResourceManagement.MapToRange(average, 0, 600, 0, 100);

        if (overallHealth <= 0)
        {
            isDead = true;
            KillPlayer();
        }
        else if (overallHealth <= 40)
        {
            isDowned = true;
        }
        else if (overallHealth <= 80)
        {
            isInjured = true;
        }

        if (currentStamina <= 0f)
            isExhausted = true;
        else
            isExhausted = false;

        if (_odm.hooksReady[0] && _odm.hooksReady[1] && !_mov.IsGrounded)
        {
            isStraining = true;
        }
        else if(!_odm.hooksReady[0] || !_odm.hooksReady[1] || _mov.IsGrounded)
        {
            isStraining = false;
        }

        SetPuppet();
    }

    void SetPuppet()
    {
        for (int i = 0; i < limbs.Length; i++)
        {
            Color desired = new Color(MapToRange(limbs[i], 0, 100, brokenColor.r, healthyColor.r), MapToRange(limbs[i], 0, 100, brokenColor.g, healthyColor.r), MapToRange(limbs[i], 0, 100, brokenColor.b, healthyColor.b), MapToRange(bloodLevel, 0, 4000, 0, 255));

            bodyPartImages[i].color = Color.Lerp(bodyPartImages[i].color,  desired, 8 * Time.deltaTime);
        }
    }

    public void DamageLimb(int limbIndex, float damageAmount)
    {
        if (damageAmount <= 1) return;

        int damageTimes = damageAmount > 100 ? 2 : 1;

        float calculatedDamageAmount = damageTimes > 2 ? damageAmount/2 : damageAmount;

        for (int times = 0; times < damageTimes; times++)
        {
            if (limbs[limbIndex] <= 0f)
            {
                limbs[limbIndex] = 0;
                if (limbs[(int)Limbs.Torso] <= 0f)
                {
                    limbs[(int)Limbs.Torso] = 0;
                    for (int i = 0; i < limbs.Length; i++)
                    {
                        if (limbs[i] <= 0)
                        {
                            limbs[i] = 0;
                            bloodLevel -= calculatedDamageAmount;
                            continue;
                        }
                        limbs[i] -= calculatedDamageAmount;
                    }
                    CalculateOverallHealth();
                }
                else
                {
                    limbs[(int)Limbs.Torso] -= calculatedDamageAmount;

                    IncreaseHeartrate(calculatedDamageAmount);
                    CalculateOverallHealth();
                }
            }
            else if (limbs[limbIndex] > 0f)
            {
                if (hasHelmet && limbIndex == limbs[(int)Limbs.Head])
                    limbs[limbIndex] -= calculatedDamageAmount;
                else
                    limbs[limbIndex] -= calculatedDamageAmount;
            }

            IncreaseHeartrate(calculatedDamageAmount);

            CalculateOverallHealth();
        }
       
    }

    public void HealLimb(int limbIndex, float healAmount)
    {
        if (limbs[limbIndex] >= 100f)
        {
            limbs[limbIndex] = 100;
            for (int i = 0; i < limbs.Length; i++)
            {
                if (limbs[limbIndex] >= 100f)
                {
                    limbs[limbIndex] = 100;
                    continue;
                }
                else if (limbs[limbIndex] < 100)
                {
                    limbs[limbIndex] += healAmount;
                }
            }
        }
        else if (limbs[limbIndex] < 100)
        {
            limbs[limbIndex] += healAmount;
        }
    }

    public void SetBleed(bool isBleeding, float bleedDamagePerTick, float timeToStop)
    {
        this.isBleeding = isBleeding;
        StartCoroutine(BleedTick(bleedDamagePerTick * MapToRange(heartrate, 60, 220, 1, 10), timeToStop));
    }

    public void NaturalHealBleed()
    {
        if (!isBleeding && bloodLevel < 4000)
        {
            bloodLevel += PL_ResourceManagement.MapToRange(heartrate, 60, 220, 0.05f, 0.1f);
        }
        else if(bloodLevel >= 4000 && !isBleeding)
        {
            HealLimb((int)Limbs.Head, PL_ResourceManagement.MapToRange(heartrate, 60, 220, 0.01f, 0.05f));
            HealLimb((int)Limbs.Torso, PL_ResourceManagement.MapToRange(heartrate, 60, 220, 0.01f, 0.05f));
        }

        CalculateOverallHealth();
    }

    public void KillPlayer()
    {
        isDead = true;

        CheckPlayerStatus();

        foreach (Behaviour beh in behavioursToDisableOnDeath)
        {
            beh.enabled = false;
        }

        _voice.KillPlayer();
        Ragdoll();
        //_mov.KillPlayer();
        _odm.KillPlayer();
        _gloc.KillPlayer();
       // _deathrgdl.KillPlayer();

      

       
    }

    public string CheckPlayerStatus()
    {
        CalculateOverallHealth();

        string status = $"Head is at {limbs[(int)Limbs.Head]} % integrity." +
            $"\n Torso is at {limbs[(int)Limbs.Torso]} % integrity." +
            $"\n Left Arm is at {limbs[(int)Limbs.LeftArm]} % integrity." +
            $"\n Right Arm is at {limbs[(int)Limbs.RightArm]} % integrity." +
            $"\n Left Leg is at {limbs[(int)Limbs.LeftLeg]} % integrity." +
            $"\n Right Leg is at {limbs[(int)Limbs.RightLeg]} % integrity." +
            $"\n Is player bleeding: {isBleeding}" +
            $"\n Is player injured: {isInjured}" +
            $"\n is player downed: {isDowned}";

        return status;
    }

    public void ReduceStamina(float amount)
    {
        if (currentStamina <= 0) return;
        currentStamina -= PL_ResourceManagement.MapToRange(heartrate, 60, 220, 0.001f, 0.004f) * amount/10;
    }

    public void IncreaseStamina(float amount)
    {
        if (currentStamina >= maxStamina) return;
        currentStamina += PL_ResourceManagement.MapToRange(heartrate, 60, 220, 0.004f, 0.001f) * amount;
    }

    public void IncreaseHeartrate(float amount)
    {
        if (heartrate >= 220) return;
        heartrate += amount * MapToRange(overallHealth, 0, 100, 0.001f, 0.005f);
    }

    public void ReduceHeartrate(float amount)
    {
        if (heartrate <= 60) return;
        heartrate -= amount * MapToRange(overallHealth, 0, 100, 0.005f, 0.001f);
    }

    public IEnumerator Heartrate()
    {
        while (!isDead)
        {
            float beatDuration = 60f / heartrate;

            // Fill from 0 to 1 over half of the beat duration
            float elapsedTime = 0f;
            while (elapsedTime < beatDuration / 2f)
            {
                float fillAmount = elapsedTime / (beatDuration / 2f);
                heartImage.fillAmount = fillAmount;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            if (!heartAudioSource.isPlaying)
            {
                heartAudioSource.volume = MapToRange(currentStamina, 0, 100, 0.3f, 0f);
                heartAudioSource.pitch = MapToRange(heartrate, 60, 220, 2f, 1f);
                heartAudioSource.PlayOneShot(heartAudioClips[0]);
            }

            // Fill from 1 back to 0 over the other half of the beat duration
            elapsedTime = 0f;
            while (elapsedTime < beatDuration / 2f)
            {
                float fillAmount = 1f - (elapsedTime / (beatDuration / 2f));
                heartImage.fillAmount = fillAmount;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            if (!heartAudioSource.isPlaying)
            {
                heartAudioSource.volume = MapToRange(currentStamina, 0, 100, 0.3f, 0f);
                heartAudioSource.pitch = MapToRange(heartrate, 60, 220, 2f, 1f);
                heartAudioSource.PlayOneShot(heartAudioClips[1]);
            }
        }
        yield break;
    }

    public IEnumerator BleedTick(float bleedPerTick, float timeToStop)
    {
        while (timeToStop > 0)
        {
            DamageLimb((int)Limbs.Head, PL_ResourceManagement.MapToRange(bloodLevel, 0, 4000, 0f, 0.05f));
            DamageLimb((int)Limbs.Torso, PL_ResourceManagement.MapToRange(bloodLevel, 0, 4000, 0f, 0.05f));

            bloodLevel -= bleedPerTick;

            timeToStop--;
            yield return new WaitForEndOfFrame();
        }
        yield break;
    }
    #endregion
    # region Equipment
    //[SerializeField, Range(0, 1000)] float maxGasAmount = 1000f;
    [SerializeField] float currentGasAmount = 800f;
    //Implement swords here for later

    public void ReplenishGas(float amount)
    {
        currentGasAmount += amount;
    }

    public void DecreaseGas(float amount)
    {
        currentGasAmount -= amount;
    }

    public static float MapToRange(float value, float minInput, float maxInput, float minOutput, float maxOutput)
    {
        // Ensure the value is clamped within the input range
        value = Mathf.Clamp(value, minInput, maxInput);

        // Calculate the ratio of the value's position within the input range
        float ratio = (value - minInput) / (maxInput - minInput);

        // Map the ratio to the output range
        float mappedValue = minOutput + ratio * (maxOutput - minOutput);

        // Return the mapped value
        return mappedValue;
    }
    #endregion
    #region Ragdoll
    public void Ragdoll()
    {
        Vector3 mainVelocity = _mov.Rigidbody.velocity;
        Vector3 mainAngularVelocity = _mov.Rigidbody.angularVelocity;
        foreach (Rigidbody rig in bodyPartsKinematicToggle)
        {
            rig.isKinematic = false;
            rig.velocity = mainVelocity;
            rig.angularVelocity = mainAngularVelocity;
        }
        ragdollAnimator.enabled = false;
    }

    public void UnRagdoll()
    {
        foreach (Rigidbody rig in bodyPartsKinematicToggle)
        {
            rig.isKinematic = true;
        }
        ragdollAnimator.enabled = true;
    }
    #endregion
}
