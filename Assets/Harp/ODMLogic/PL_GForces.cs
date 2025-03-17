using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Player.Movement;

public class PL_GForces : MonoBehaviour
{
    private Vector3 lastVelocity;
    public Vector3 gForces;
    [SerializeField]public float gForcesAverage;
    [SerializeField]public Vector3 gLocVector;
    [Header("References")]
    public Rigidbody rb;
    public PL_ODM playerODMGear;
    public PlayerMotor playerMove;
    public PL_ResourceManagement plres;

    public float updateInterval = 0.5f; // Time in seconds between updates
    private float nextUpdateTime = 0f;

    // Public floats to access g-forces externally
    [Header("UI")]
    public Image staminaImage;
    public Image overallHealthImage;
    public Canvas playerGUI;
    float speedKmph;
    public TextMeshProUGUI speedKmphText;
    public Image speedKmphImg;
    float currentGForceUp;
    float targetGForceUp;
    public TextMeshProUGUI gForceUpText;
    float currentGForceDown;
    float targetGForceDown;
    public TextMeshProUGUI gForceDownText;
    float currentGForceRight;
    float targetGForceRight;
    public TextMeshProUGUI gForceRightText;
    float currentGForceLeft;
    float targetGForceLeft;
    public TextMeshProUGUI gForceLeftText;
    float currentGForceForward;
    float targetGForceForward;
    public TextMeshProUGUI gForceForwardText;
    float currentGForceBack;
    float targetGForceBack;
    public TextMeshProUGUI gForceBackText;

    [Header("Post Processing")]
    public Volume redOutPostProcess;
    public Volume blackOutPostProcess;
    public Volume strainPostProcess;
    public Volume deadPostProcessing;

    [Header("Audio")]
    public AudioSource windSpeedAudioSource;
    public AudioSource ropeCreakAudioSource;
    public AudioSource impactAudioSource;
    public AudioSource bodyPartImpactAudioSource;
    public AudioClip[] impactAudioClips;
    public AudioClip[] hardImpactAudioClips;
    public AudioClip[] boneCrackleAudioClips;
    public Rigidbody[] bodyPartsForImpactAudio;
    public AudioLowPassFilter lpfStraining;

    [Header("Test")]
    public AudioHighPassFilter musicHpfTest;

    public bool unsafeAny = false;

    [Header("Blackout")]
    public bool blackout;

    [Header("Redout")]
    public bool redout;

    public bool straining;

    public bool deadlyGForce;

    public float lerpSpeed = 1f;

    [SerializeField] bool isDead = false;

    private void Update()
    {
        if (isDead) return;
        
        // Calculate proportional weights for blacking out from each direction
        float blackOutUpWeight = PL_ResourceManagement.MapToRange(currentGForceUp, 3.5f, 10.5f, 0f, 1f);
        float blackOutLeftWeight = PL_ResourceManagement.MapToRange(currentGForceLeft, 3.5f, 10.5f, 0f, 1f);
        float blackOutRightWeight = PL_ResourceManagement.MapToRange(currentGForceRight, 3.5f, 10.5f, 0f, 1f);
        float blackOutForwardWeight = PL_ResourceManagement.MapToRange(currentGForceForward, 3.5f, 10.5f, 0f, 1f);
        float blackOutBackWeight = PL_ResourceManagement.MapToRange(currentGForceBack, 3.5f, 10.5f, 0f, 1f);

        // Combine the weights to determine the overall black out weight
        float overallBlackOutWeight = Mathf.Max(blackOutUpWeight, blackOutLeftWeight, blackOutRightWeight, blackOutForwardWeight, blackOutBackWeight);

        // Smoothly interpolate the post-processing weight for blacking out
        blackOutPostProcess.weight = Mathf.Lerp(blackOutPostProcess.weight, overallBlackOutWeight, 10 * Time.deltaTime);

        redOutPostProcess.weight = Mathf.Lerp(redOutPostProcess.weight, PL_ResourceManagement.MapToRange(currentGForceDown, 1.5f, 4.5f, 0f, 1f), Time.deltaTime * 10f);

        if (straining && !playerODMGear.hooksReady[0] || straining && !playerODMGear.hooksReady[1])
        {
            ropeCreakAudioSource.volume = Mathf.Lerp(ropeCreakAudioSource.volume, 0.8f, Time.fixedDeltaTime * 3f);
        }
        else if (!straining || playerODMGear.hooksReady[0] && playerODMGear.hooksReady[1])
        {
            ropeCreakAudioSource.volume = Mathf.Lerp(ropeCreakAudioSource.volume, 0, Time.fixedDeltaTime * 4f);
        }

        if (playerODMGear.hooksReady[0] && playerODMGear.hooksReady[1] && !playerMove.IsGrounded && rb.velocity.y <= -1f)
        {
            musicHpfTest.cutoffFrequency = Mathf.Lerp(musicHpfTest.cutoffFrequency, 1000f, Time.deltaTime);
        }
        else
        {
            musicHpfTest.cutoffFrequency = Mathf.Lerp(musicHpfTest.cutoffFrequency, 0f, 8 * Time.deltaTime);
        }

        LerpAndCheckState();

        if (Time.time >= nextUpdateTime)
        {
            ProcessGForces();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    void FixedUpdate()
    {
        if(isDead) return;
        

        overallHealthImage.fillAmount = plres.GetCurrentOverallHealth()/100;

        staminaImage.fillAmount = plres.GetCurrentStamina()/100;

        CalculateSpeed();

        CalculateGForces();

        if (straining)
        {
            plres.ReduceStamina((gForcesAverage + 1) * 100);
            plres.IncreaseHeartrate(gForcesAverage + 0.1f);
        }
        else
        {
            plres.IncreaseStamina(1.5f);
            plres.ReduceHeartrate(0.1f);
        }

        if(rb.velocity.sqrMagnitude <= 10)
        {
            windSpeedAudioSource.volume = 0;
            windSpeedAudioSource.pitch = 1;
        }
        else
        {
            windSpeedAudioSource.volume = Mathf.Lerp(windSpeedAudioSource.volume, PL_ResourceManagement.MapToRange(rb.velocity.sqrMagnitude, 10, 1500, 0, 0.6f), Time.fixedDeltaTime * 8f);
            windSpeedAudioSource.pitch = Mathf.Lerp(windSpeedAudioSource.pitch, PL_ResourceManagement.MapToRange(rb.velocity.sqrMagnitude, 10, 1500, 0.8f, 1.5f), Time.fixedDeltaTime * 4f);
        }

        lpfStraining.cutoffFrequency = Mathf.Lerp(lpfStraining.cutoffFrequency, PL_ResourceManagement.MapToRange(gForcesAverage, 0, 0.2f, 20000f, 2000f), Time.fixedDeltaTime * 4);

        lastVelocity = rb.velocity;
    }

    void ProcessGForces()
    {
        //if (currentGForceUp > 4f && unsafeAny)
        //{
        //    plres.ReduceStamina((0.3f + gForcesAverage) * 100);
        //    plres.ReduceHeartrate(0.00001f);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.Head, gForcesAverage * 50);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.Torso, gForcesAverage * 50);
        //}
        //else if (unsafeAny)
        //{
        //    plres.ReduceStamina((0.2f + gForcesAverage) * 100);
        //    plres.ReduceHeartrate(0.00001f);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.Torso, gForcesAverage * 25);
        //}

        //if (deadlyGForce)
        //{
        //    plres.ReduceStamina((0.5f + gForcesAverage));
        //    plres.IncreaseHeartrate(gForcesAverage * 2);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.Head, gForcesAverage);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.Torso, gForcesAverage);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.LeftLeg, gForcesAverage);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.RightLeg, gForcesAverage);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.LeftArm, gForcesAverage);
        //    plres.DamageLimb((int)PL_ResourceManagement.Limbs.RightArm, gForcesAverage);

        //    if (!impactAudioSource.isPlaying)
        //    {
        //        impactAudioSource.volume = PL_ResourceManagement.MapToRange(gForcesAverage, 0f, 1f, 0.1f, 1f);
        //        impactAudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
        //        impactAudioSource.PlayOneShot(boneCrackleAudioClips[UnityEngine.Random.Range(0, boneCrackleAudioClips.Length)]);
        //    }
        //}
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        int random = UnityEngine.Random.Range(0, 1);

        bool bleed = random == 1 ? true : false;

        // Get the relative velocity of the collision along the collision normal
        Vector3 relativeVelocity = collision.relativeVelocity;
        float impactSpeed = Vector3.Dot(relativeVelocity, collision.contacts[0].normal);

        if (impactSpeed > 10)
        {
            // Calculate the impact force based on the relative velocity
            float impactForce = impactSpeed * playerODMGear.movementScript.Rigidbody.mass;

            // Determine the appropriate audio clip and volume based on the impact force
            AudioClip[] clipsToUse = impactForce >= 25 ? hardImpactAudioClips : impactAudioClips;
            float volume = PL_ResourceManagement.MapToRange(impactForce, 10, 50 * 2, 0.4f, 0.7f);

            // Play the selected audio clip
            int randomIndex = UnityEngine.Random.Range(0, clipsToUse.Length);
            if (!impactAudioSource.isPlaying)
            {
                impactAudioSource.volume = volume;
                impactAudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
                impactAudioSource.PlayOneShot(clipsToUse[randomIndex]);
            }

            // Do damage based on force
            int randomLimb = UnityEngine.Random.Range(0, 5);
            plres.DamageLimb(randomLimb, impactForce*7);

            // Bleeding for 5 seconds if the impact was worth bleeding
            if (bleed)
                plres.SetBleed(bleed, 0.001f, 5f);
        }
    }

    void CalculateSpeed()
    {
        // Calculate speed in meters per second (m/s)
        float speedMps = GetComponent<Rigidbody>().velocity.magnitude;

        // Convert speed to kilometers per hour (km/h)
        speedKmph = speedMps * 3.6f;

        speedKmph = Mathf.Clamp(speedKmph, 0, 200);

        speedKmphText.text = Mathf.Round(speedKmph).ToString() + " KM/H";

        speedKmphImg.fillAmount = PL_ResourceManagement.MapToRange(speedKmph, 0, 200, 0, 1);
    }

    void LerpAndCheckState()
    {
        // Smoothly interpolate current g-force values towards the target values
        currentGForceUp = Mathf.Lerp(currentGForceUp, targetGForceUp, lerpSpeed * Time.deltaTime);
        currentGForceDown = Mathf.Lerp(currentGForceDown, targetGForceDown, lerpSpeed * Time.deltaTime);
        currentGForceRight = Mathf.Lerp(currentGForceRight, targetGForceRight, lerpSpeed * Time.deltaTime);
        currentGForceLeft = Mathf.Lerp(currentGForceLeft, targetGForceLeft, lerpSpeed * Time.deltaTime);
        currentGForceForward = Mathf.Lerp(currentGForceForward, targetGForceForward, lerpSpeed * Time.deltaTime);
        currentGForceBack = Mathf.Lerp(currentGForceBack, targetGForceBack, lerpSpeed * Time.deltaTime);

        gForceUpText.text = Math.Round(currentGForceUp, 0).ToString();
        gForceDownText.text = Math.Round(currentGForceDown, 0).ToString();
        gForceLeftText.text = Math.Round(currentGForceLeft, 0).ToString();
        gForceRightText.text = Math.Round(currentGForceRight, 0).ToString();
        gForceForwardText.text = Math.Round(currentGForceForward, 0).ToString();
        gForceBackText.text = Math.Round(currentGForceBack, 0).ToString();
    }

    void CalculateGForces()
    {
        // Calculate the change in velocity (Delta V)
        Vector3 deltaV = rb.velocity - lastVelocity;

        // Calculate acceleration (a = Delta V / Delta t)
        Vector3 acceleration = deltaV / Time.fixedDeltaTime;

        // Calculate g-forces (g = a / g_earth)
        gForces = acceleration / 9.81f;

        gForcesAverage = (gForces.x + gForces.y + gForces.z) / 3;
        gForcesAverage = PL_ResourceManagement.MapToRange(Mathf.Clamp(Mathf.Abs(gForcesAverage), 0, 10), 0, 10, 0, 1);

        gLocVector = Vector3.Lerp(gLocVector, new Vector3(gForces.x, gForces.y, gForces.z), 10 * Time.deltaTime);

        // Calculate and round target g-forces
        targetGForceUp = Mathf.Abs(Mathf.Round(Mathf.Max(0, gForces.y)));
        targetGForceDown = Mathf.Abs(Mathf.Round(Mathf.Min(0, gForces.y)));
        targetGForceRight = Mathf.Abs(Mathf.Round(Mathf.Max(0, gForces.x)));
        targetGForceLeft = Mathf.Abs(Mathf.Round(Mathf.Min(0, gForces.x)));
        targetGForceForward = Mathf.Abs(Mathf.Round(Mathf.Max(0, gForces.z)));
        targetGForceBack = Mathf.Abs(Mathf.Round(Mathf.Min(0, gForces.z)));

        if(currentGForceUp >= 8 && !unsafeAny)
        {
            
            unsafeAny = true;
        }
        else if (currentGForceDown >= 4 && !unsafeAny)
        {
            unsafeAny = true;
        }
        else if (currentGForceLeft > 8 && !unsafeAny || currentGForceRight > 8 && !unsafeAny)
        {
            
            unsafeAny = true;
        }
        else if (currentGForceForward > 8 && !unsafeAny || currentGForceBack > 8 && !unsafeAny)
        {
            
            unsafeAny = true;
        }
        else if (currentGForceUp < 8 && currentGForceDown < 4 && currentGForceLeft < 8 && currentGForceRight < 8 && currentGForceForward < 8 && currentGForceBack < 8)
        {
            unsafeAny = false;
        }

        if (currentGForceUp >= 4.5f || currentGForceDown >= 2.5f || currentGForceLeft >= 4.5f || currentGForceRight >= 4.5f || currentGForceForward >= 4.5f || currentGForceBack >= 4.5f) 
        {
            straining = true;
        }
        else if(currentGForceUp < 4.5f && currentGForceDown < 2.5f && currentGForceLeft < 4.5f && currentGForceRight < 4.5f && currentGForceForward < 4.5f && currentGForceBack < 4.5f && playerMove.IsGrounded)
        {
            straining = false;
        }

        if (currentGForceUp >= 14 || currentGForceDown >= 14 || currentGForceLeft >= 14 || currentGForceRight >= 14 || currentGForceForward >= 14 || currentGForceBack >= 14)
        {
            deadlyGForce = true;
            ProcessGForces();
        }
        else
        {
            deadlyGForce = false;
        }
    }
    public void KillPlayer()
    {
        isDead = true;

        playerGUI.enabled = false;

        deadPostProcessing.weight = 0f;
        strainPostProcess.weight = 0f;
        blackOutPostProcess.weight = 0.2f;
        redOutPostProcess.weight = 0.2f;

        lpfStraining.cutoffFrequency = 2000f;

        ropeCreakAudioSource.Stop();
        windSpeedAudioSource.Stop();
        this.enabled = false;
    }
}
