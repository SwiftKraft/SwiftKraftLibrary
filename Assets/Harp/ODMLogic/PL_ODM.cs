using Player.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class PL_ODM : MonoBehaviour
{
    [SerializeField]
    private float currentSpeed; // Private variable to store speed
    [SerializeField]
    private bool isReeling = false;
    [SerializeField]
    private bool isOrbiting = false;
    [SerializeField]
    private bool isProperlyHooked = true;
    PlayerMovementSlide slide;
    PlayerMovementGround ground;
 
    

    Vector3 leftDirection;
    Vector3 rightDirection;

    [Header("Main")]

    public float hookEjectForce = 2;
    
    public float hookCurrentReelInForce = 2;
    public float hookNormalReelInForce = 2;
    public float hookBoostReelInForce = 2;
    
    public float hookMaxDistance = 100;
    public Transform playerTransform;
    public Transform playerCameraTransform;
    public PlayerMotor movementScript;

    [Header("Audio Hook Fire")]
    public List<AudioSource> hookFireAudioSources;
    public List<AudioClip> hookFireAudioClips;

    [Header("Audio Latch and Reel")]
    public List<AudioSource> hookReelAudioSources;
    public List<AudioSource> hooksLatchAudioSources;
    public List<float> targetPitch = new List<float>(new float[] { 1f, 1f });
    float divider;

    [Header("Audio Gas ODM")]
    public AudioSource gasAudioSource;

    [Header("Audio Dash ODM")]
    public AudioSource gasDashAudioSource;

    [Header("Particles Gas ODM")]
    public ParticleSystem gasParticles;

    [Header("Particle Dash ODM")]
    public ParticleSystem gasDashParticles;

    [Header("Logic Gas ODM")]
    public float currentGasAmount = 1000;
    public float gasForce = 15;
    public float swingForce = 5;
    public bool isUsingGas = false;

    [Header("Logic Hook Separation")]
    public float separation = 0;
    public float maxAngle = 45f;
    public float currentSeparation = 0.2f;

    [Header("Logic Dash")]
    public float dashTimer = 1f;
    public float gasDashForce = 15f;
    public float dashCooldown = 1f;
    public float doubleTapThreshold = 0.3f;
    public List<float> lastTapTime = new List<float>(new float[] { 1, 1, 1, 1, 1 });

    [Header("Logic Hook Fire")]
    public List<bool> hooksReady = new List<bool>(new bool[] { true, true });
    public List<float> hookCooldownTimes = new List<float>(new float[] { 0.5f, 0.5f });
    public float hookFireCooldownTimeBase = 0.5f;
    public List<int> reelingInOutState = new List<int>(new int[] { 3, 3 });
    public List<Vector3> hookSwingPoints = new List<Vector3>(new Vector3[] { Vector3.zero, Vector3.zero });
    public List<Vector3> hookPositions = new List<Vector3>(new Vector3[] { Vector3.zero, Vector3.zero });
    public List<Transform> hookStartTransforms = new List<Transform>(new Transform[] { null, null });
    public List<SpringJoint> hookJoints = new List<SpringJoint>(new SpringJoint[] { null, null });

    

    [Header("Hook Fire Visual")]
    public List<LineRenderer> hookWireRenderers = new List<LineRenderer>(new LineRenderer[] { null, null });

    [Header("Logic Prediction")]
    public LayerMask grappleSurfaces;
    public LayerMask nonGrappleSurfaces;
    public float predictionSeparationNegation = 8f;
    public float predictionSphereRadius = 15f;
    public List<RaycastHit> hookPredictionHits = new List<RaycastHit>(new RaycastHit[] { new RaycastHit(), new RaycastHit() });

    [Header("UI Prediction")]
    public List<UnityEngine.UI.Image> hookCrosshairs;
    public List<UnityEngine.UI.Image> hookStaticCrosshairs;

    [Header("UI Gas")]
    public UnityEngine.UI.Image gasUI;
    [Header("UI Speed")]
    public TMP_Text SpeedText;

    void Update()
    {
        currentSpeed = Mathf.Ceil(movementScript.Rigidbody.velocity.magnitude);
        SpeedText.text = currentSpeed.ToString() + " km/h";
        UpdateCooldownTimers();
        UpdateDashTimers();
        UpdateSpringSettings(0);
        UpdateSpringSettings(1);
        CheckInputUpdate();
        ReelingSounds(0);

        ReelingSounds(1);
    }

    private void UpdateGasUI()
    {
        gasUI.fillAmount = currentGasAmount / 1500;
    }

    void FixedUpdate()
    {
        PredictGrappleSpot(0);
       
        PredictGrappleSpot(1);
        CheckInputFixed();
        UpdateGasUI();
        CheckHookLocked(1);

    }

    void UpdateCooldownTimers()
    {
        if (hookCooldownTimes[0] > 0)
            hookCooldownTimes[0] -= Time.deltaTime;

        if (hookCooldownTimes[1] > 0)
            hookCooldownTimes[1] -= Time.deltaTime;
    }

    void UpdateDashTimers()
    {
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
        }
    }

    

    void PredictGrappleSpot(int hookIndex)
    {
        if (!hooksReady[hookIndex])
        {
            hookCrosshairs[hookIndex].rectTransform.position = playerCameraTransform.gameObject.GetComponent<Camera>().WorldToScreenPoint(hookPositions[hookIndex]);
        }

        //Check if the hooks are ready
        if (!hooksReady[hookIndex]) return;
        RaycastHit spherecastHit = new RaycastHit();
        RaycastHit raycastHit = new RaycastHit();
        Vector3 realHitVector = Vector3.zero;

        float currentAngle = Mathf.Lerp(-maxAngle, maxAngle, (currentSeparation + 1) / 2);  // Map separation to angle

        Vector3 leftDirection = Quaternion.AngleAxis(-currentAngle, playerCameraTransform.up) * playerCameraTransform.forward;

        Vector3 rightDirection = Quaternion.AngleAxis(currentAngle, playerCameraTransform.up) * playerCameraTransform.forward;

        // Shoot hooks based on the current index
        if (hookIndex == 0)  // Left hook
        {
            Physics.SphereCast(playerCameraTransform.position, predictionSphereRadius, leftDirection, out spherecastHit, hookMaxDistance, grappleSurfaces);
            Physics.Raycast(playerCameraTransform.position, leftDirection, out raycastHit, hookMaxDistance, grappleSurfaces);
        }
        else if (hookIndex == 1)  // Right hook
        {
            Physics.SphereCast(playerCameraTransform.position, predictionSphereRadius, rightDirection, out spherecastHit, hookMaxDistance, grappleSurfaces);
            Physics.Raycast(playerCameraTransform.position, rightDirection, out raycastHit, hookMaxDistance, grappleSurfaces);
        }

        //if raycast hit anything
        if (raycastHit.collider != null)
        {
            if (Physics.Linecast(playerCameraTransform.position, raycastHit.point, out RaycastHit hit, nonGrappleSurfaces))
            {
                realHitVector = Vector3.zero;
            }
            else
                realHitVector = raycastHit.point;
        }
        //if raycast hit nothing
        else if (spherecastHit.collider != null)
        {
            if (Physics.Linecast(playerCameraTransform.position, spherecastHit.point, out RaycastHit hit, nonGrappleSurfaces))
            {
                realHitVector = Vector3.zero;
            }
            else
                realHitVector = spherecastHit.point;
        }

        //if either of the hits hit anything
        if (realHitVector != Vector3.zero)
        {
            hookPredictionHits[hookIndex] = raycastHit.point == Vector3.zero ? spherecastHit : raycastHit;
            hookCrosshairs[hookIndex].gameObject.SetActive(true);
            hookCrosshairs[hookIndex].rectTransform.position = playerCameraTransform.gameObject.GetComponent<Camera>().WorldToScreenPoint(hookPredictionHits[hookIndex].point);
        }
        //if either of the hits hit nothing
        else
        {
            RaycastHit tempHit = new RaycastHit();
            tempHit.point = Vector3.zero;

            hookPredictionHits[hookIndex] = tempHit;
            hookCrosshairs[hookIndex].gameObject.SetActive(false);
        }

        hookStaticCrosshairs[0].rectTransform.position = playerCameraTransform.gameObject.GetComponent<Camera>().WorldToScreenPoint(playerCameraTransform.position + leftDirection);
        hookStaticCrosshairs[1].rectTransform.position = playerCameraTransform.gameObject.GetComponent<Camera>().WorldToScreenPoint(playerCameraTransform.position + rightDirection);
    }

    public static float MapToRange(float value, float minInput, float maxInput, float minOutput, float maxOutput)
    {
        Debug.Log("CONVERTING");
        // Ensure the value is clamped within the input range
        value = Mathf.Clamp(value, minInput, maxInput);

        // Calculate the ratio of the value's position within the input range
        float ratio = (value - minInput) / (maxInput - minInput);

        // Map the ratio to the output range
        float mappedValue = minOutput + ratio * (maxOutput - minOutput);

        // Return the mapped value
        return mappedValue;
    }

    void UpdateSpringSettings(int hookIndex)
    {
        if (hooksReady[hookIndex] || !hookJoints[hookIndex])//this has been disabled. add a ! in front of "hooksready" to reenable logic
        {
           
            return;
        }

        


        if (Vector3.Distance(movementScript.Rigidbody.transform.position, hookSwingPoints[hookIndex]) >= 5f && !isReeling)
        {
            Debug.Log("CONVERTING");
            
            hookJoints[hookIndex].tolerance = 0.025f;
            hookJoints[hookIndex].spring = MapToRange(movementScript.Rigidbody.velocity.sqrMagnitude, 1, 300, 7.5f, 20f);
            hookJoints[hookIndex].damper = MapToRange(movementScript.Rigidbody.velocity.sqrMagnitude, 1, 50, 2.5f, 10f);
            hookJoints[hookIndex].massScale = MapToRange(movementScript.Rigidbody.velocity.sqrMagnitude, 1, 50, 4.5f, 2f);
            
        }
        else
        {
            
            Debug.Log("1");
            /*
            hookJoints[hookIndex].tolerance = 0.025f;
            hookJoints[hookIndex].spring = 20;
            hookJoints[hookIndex].damper = 10;
            hookJoints[hookIndex].massScale = 4.5f;
            */
            hookJoints[hookIndex].tolerance = 0f;
            hookJoints[hookIndex].spring = 0;
            hookJoints[hookIndex].damper = 0;
            hookJoints[hookIndex].massScale = 0f;


        }

       

      
    }

    void CheckInputUpdate()
    {
        // Hook fire detection
        if (Input.GetMouseButtonDown(0) && hookCooldownTimes[0] <= 0 && hooksReady[0] && hookPredictionHits[0].point != Vector3.zero)
        {
            FireHook(0);
            
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopHook(0);
           
        }

        if (Input.GetMouseButtonDown(1) && hookCooldownTimes[1] <= 0 && hooksReady[1] && hookPredictionHits[1].point != Vector3.zero)
        {
            FireHook(1);
           
        }
        else if (Input.GetMouseButtonUp(1))
        {
            StopHook(1);
           
        }

        // Gas particles
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!isReeling)
            {
                StartODMGasVFX();
            }
                
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            StopODMGasVFX();
        }

        // Dashing
        
        if (Input.GetKeyDown(KeyCode.A) )
        {
            HandleDash(0);

        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            HandleDash(1);
        }
        if (Input.GetKeyDown(KeyCode.W) )
        {
            HandleDash(2);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            HandleDash(3);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //HandleDash(4);
           //Big Jump 

        }

        //Orbiting
        if (movementScript.IsGrounded == false) //Player distance from point is too long  )
        {
           if (!hooksReady[0] || !hooksReady[1])
            {


                isOrbiting = false;


                if (Input.GetKey(KeyCode.W) && isReeling)
                    {
                        HandleDashNoDoubleTap(5);
                    isOrbiting = true;

                    }
                    if (Input.GetKey(KeyCode.S) && isReeling)
                    {
                        HandleDashNoDoubleTap(6);
                    isOrbiting = true;
                }
                    if (Input.GetKey(KeyCode.A) && isReeling)
                    {
                        HandleDashNoDoubleTap(7);
                    isOrbiting = true;
                }
                    if (Input.GetKey(KeyCode.D) && isReeling)
                    {
                        HandleDashNoDoubleTap(8);
                    isOrbiting = true;
                }
                
        }
        }
        else
            return;
        


        // Adjust separation
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            separation = Mathf.Clamp(separation + 0.1f, 0f, 1f);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            separation = Mathf.Clamp(separation - 0.1f, 0f, 1f);
        }

        // Smooth separation
        if (currentSeparation < 0.9f)
        {
            currentSeparation = Mathf.Lerp(currentSeparation, separation, 8 * Time.deltaTime);
        }
        else if (currentSeparation > 0.1f)
        {
            currentSeparation = Mathf.Lerp(currentSeparation, separation, 8 * Time.deltaTime);
        }
        else if (currentSeparation >= 0.9f)
        {
            currentSeparation = 1;
        }
        else if (currentSeparation <= 0.1f)
        {
            currentSeparation = 0;
        }
    }

    void HandleDash(int buttonIndex)
    {
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            return;
        }

        
            if (Time.time - lastTapTime[buttonIndex] < doubleTapThreshold)
            {
                PerformDash(buttonIndex);
                dashTimer = dashCooldown;
            }
        
        
        

        lastTapTime[buttonIndex] = Time.time;
    }
    void HandleDashNoDoubleTap(int buttonIndex)
    {
       


        
            PerformDash(buttonIndex);
            dashTimer = dashCooldown;

       



        
    }

    IEnumerator OrbitVelocityChange()
    {
        Vector3 previousVelocity = movementScript.Rigidbody.velocity; // Store current velocity

        Vector3 currentVelocity = movementScript.Rigidbody.velocity.normalized;

        Vector3 newVelocity = Vector3.Lerp(previousVelocity, currentVelocity, Time.deltaTime * 1f); // Smoothly transition[The greater the f value the stronger the lerp]

        movementScript.Rigidbody.velocity = newVelocity; // Apply smooth transition

        yield return null;
    }
    void PerformDash(int buttonIndex)
    {
        if (currentGasAmount < 0) return;

        switch (buttonIndex)
        {
            case 0: // Left Dash
                movementScript.Rigidbody.AddForce(-playerCameraTransform.right * gasDashForce * 2, ForceMode.VelocityChange);
                break;
            case 1: // Right Dash
                movementScript.Rigidbody.AddForce(playerCameraTransform.right * gasDashForce * 2, ForceMode.VelocityChange);
                break;
            case 2: // Forward Dash
                movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.forward * gasDashForce * 2, ForceMode.VelocityChange);
                break;
            case 3: // Back Dash
                movementScript.Rigidbody.AddForce(-movementScript.Rigidbody.transform.forward * gasDashForce * 2, ForceMode.VelocityChange);
                break;
            case 4: // Up Dash
                movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.up * gasDashForce / 1.4f, ForceMode.VelocityChange);
                break; ///Up Down Left Right Forces for when hooked, gas enabled, and holding WASD keys need to be created.






            case 5: // Up Orbit
                StartCoroutine(OrbitVelocityChange());
                movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.up * gasDashForce / 11f, ForceMode.VelocityChange);
                break;
            case 6: // Down Orbit
                StartCoroutine(OrbitVelocityChange());
                movementScript.Rigidbody.AddForce(-movementScript.Rigidbody.transform.up * gasDashForce / 11f, ForceMode.VelocityChange);
                break;
            case 7: // Left Orbit
                StartCoroutine(OrbitVelocityChange());
                movementScript.Rigidbody.AddForce(-movementScript.Rigidbody.transform.right * gasDashForce / 11f, ForceMode.VelocityChange);
                break;
            case 8: // Right Orbit
                StartCoroutine(OrbitVelocityChange());
                movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.right * gasDashForce / 11f, ForceMode.VelocityChange);
                

                
                break;
        }
        
        

        
        currentGasAmount -= gasDashForce / 100;
        gasDashParticles.Emit(120);
        gasDashParticles.Play();
        //gasDashAudioSource.Play();
    }

    void CheckInputFixed()
    {

        // Gas usage
        if (Input.GetKey(KeyCode.LeftShift) && movementScript.IsGrounded == false && !isReeling)
        {
            if (currentGasAmount < 0) return;

            UseGas(gasForce);
            movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.up * 0.09f, ForceMode.VelocityChange);
            //  movementScript.Rigidbody.AddForce(playerCameraTransform.transform.forward* 0.03f, ForceMode.VelocityChange);
        }
        else if (isUsingGas)
        {
            StopUseGas(swingForce);
            isUsingGas = false;
        }

        // Hook reeling
        if (isProperlyHooked == true)
    {
        if (hookJoints[0])
        {

            ReelInHook(0);
        }
        if (hookJoints[1])
        {
            ReelInHook(1);
        }
    }
        else
        {
            isReeling = false;
        }
    }

    

    void ReelInHook(int hookIndex)
    {
        if (isProperlyHooked && Input.GetKey(KeyCode.Space))//Gas Boost to reel speed CHANGE TO SPACEBAR
        {
            hookCurrentReelInForce = hookBoostReelInForce;
        }
        else 
        {
            hookCurrentReelInForce = hookNormalReelInForce;
        }
        

        if (currentGasAmount <= 0) return;

        Vector3 previousVelocity = movementScript.Rigidbody.velocity; // Store current velocity
        if (!isReeling)
        {
            isReeling = true;
            movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.up * 0.1f, ForceMode.Impulse);
            
        }


        float distanceFromPoint = Vector3.Distance(transform.position, hookSwingPoints[hookIndex]);
        float targetMaxDistance = Mathf.Max(0.1f, distanceFromPoint * 0.7f);




        if (distanceFromPoint > 0.0f)
        {
            divider = Mathf.Lerp(divider, PL_ResourceManagement.MapToRange(distanceFromPoint, 0, hookMaxDistance, 0.1f, 0.01f), Time.deltaTime * 4f);

            Vector3 reelForceBasedOnDistance = (hookSwingPoints[hookIndex] - transform.position).normalized * (hookCurrentReelInForce * divider);
            Vector3 newVelocity = Vector3.Lerp(previousVelocity, reelForceBasedOnDistance, Time.deltaTime * 3f); // Smoothly transition[The greater the f value the stronger the lerp]

            movementScript.Rigidbody.velocity = newVelocity; // Apply smooth transition
            movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.up * 0.1f, ForceMode.VelocityChange);


        }
        else
            Debug.Log("hooktooshort");
        

        currentGasAmount -= 0.1f;
    }

    void ReelingSounds(int hookIndex)
    {
        if (reelingInOutState[hookIndex] == 0)
        {
            if (hookSwingPoints[hookIndex] == Vector3.zero) return;
            targetPitch[hookIndex] = PL_ResourceManagement.MapToRange(Vector3.Distance(hookStartTransforms[hookIndex].position, hookPositions[hookIndex]), 0, 100, 0.8f, 1.5f);
            hookReelAudioSources[hookIndex].volume = Mathf.Lerp(hookReelAudioSources[hookIndex].volume, 0.4f, Time.deltaTime * (hookEjectForce * 4));
            hookReelAudioSources[hookIndex].pitch = Mathf.Lerp(hookReelAudioSources[hookIndex].pitch, targetPitch[hookIndex], Time.deltaTime * (hookEjectForce * 4));
        }
        else if (reelingInOutState[hookIndex] == 1)
        {
            if (hookSwingPoints[hookIndex] == Vector3.zero) return;
            Vector3 velocity = movementScript.Rigidbody.velocity;
            float speedInHookAxis = Vector3.Dot(velocity, (hookStartTransforms[hookIndex].position - hookPositions[hookIndex]));
            targetPitch[hookIndex] = PL_ResourceManagement.MapToRange(Mathf.Abs(speedInHookAxis), 0, 200, 0.8f, 1.5f);
            hookReelAudioSources[hookIndex].volume = Mathf.Lerp(hookReelAudioSources[hookIndex].volume, PL_ResourceManagement.MapToRange(Mathf.Abs(speedInHookAxis), 0, 100, 0f, 0.4f), Time.deltaTime * (hookEjectForce * 4));
            hookReelAudioSources[hookIndex].pitch = Mathf.Lerp(hookReelAudioSources[hookIndex].pitch, targetPitch[hookIndex], Time.deltaTime * (hookEjectForce * 4));
        }
        else if (reelingInOutState[hookIndex] == 2)
        {
            if (hookSwingPoints[hookIndex] == Vector3.zero) return;
            targetPitch[hookIndex] = PL_ResourceManagement.MapToRange(Vector3.Distance(hookStartTransforms[hookIndex].position, hookPositions[hookIndex]), 0, 100, 0.8f, 1.5f);
            hookReelAudioSources[hookIndex].volume = Mathf.Lerp(hookReelAudioSources[hookIndex].volume, 0.4f, Time.deltaTime * (hookEjectForce * 4));
            hookReelAudioSources[hookIndex].pitch = Mathf.Lerp(hookReelAudioSources[hookIndex].pitch, targetPitch[hookIndex], Time.deltaTime * (hookEjectForce * 4));
        }
        else if (reelingInOutState[hookIndex] == 3)
        {
            hookReelAudioSources[hookIndex].volume = Mathf.Lerp(hookReelAudioSources[hookIndex].volume, 0, Time.deltaTime * (hookEjectForce * 10));
        }
    }

    void UseGas(float force)
    {
        if (currentGasAmount < 0) return;


        isUsingGas = true;
        gasAudioSource.volume = Mathf.Lerp(gasAudioSource.volume, 0.3f, Time.deltaTime * 8f);
        currentGasAmount -= 0.1f;

        Vector3 wishDir = movementScript.GetWishDir();
        Vector3 moveDirection = wishDir;
        moveDirection.y = 0.25f;
        movementScript.Rigidbody.AddForce(moveDirection * force, ForceMode.Acceleration);
    }

    void StopUseGas(float force)
    {
        gasAudioSource.volume = Mathf.Lerp(gasAudioSource.volume, 0, Time.deltaTime * 8f);
        Vector3 wishDir = movementScript.GetWishDir();
        Vector3 moveDirection = wishDir;
        moveDirection.y = 0f;
        movementScript.Rigidbody.AddForce(moveDirection * force, ForceMode.Acceleration);
    }

    void StartODMGasVFX()
    {
        if (currentGasAmount > 0)
        {
            gasParticles.Emit(20);
            gasParticles.Play();
        }
        else
        {
            gasParticles.Stop();
            gasAudioSource.volume = Mathf.Lerp(gasAudioSource.volume, 0, Time.deltaTime * 8f);
        }
    }

    void StopODMGasVFX()
    {
        gasParticles.Stop();
        gasAudioSource.volume = 0;
    }

    void FireHook(int hookIndex)
    {
        PlayHookFireSound(hookFireAudioSources[hookIndex], hookFireAudioClips[0]);
        hookCooldownTimes[hookIndex] = hookFireCooldownTimeBase;
        reelingInOutState[hookIndex] = 0;

        if (hookPredictionHits[hookIndex].point == Vector3.zero) return;

        hookSwingPoints[hookIndex] = hookPredictionHits[hookIndex].point;
        hookPositions[hookIndex] = hookStartTransforms[hookIndex].position;
        float distanceFromPoint = Vector3.Distance(playerTransform.position, hookSwingPoints[hookIndex]);

        hookJoints[hookIndex] = playerTransform.gameObject.AddComponent<SpringJoint>();
        hookJoints[hookIndex].autoConfigureConnectedAnchor = false;
        hookJoints[hookIndex].connectedAnchor = hookSwingPoints[hookIndex];
        hookJoints[hookIndex].spring = 0;
        hookJoints[hookIndex].damper = 0;
        hookJoints[hookIndex].massScale = 0;
        hookJoints[hookIndex].maxDistance = distanceFromPoint;
        hookJoints[hookIndex].minDistance = 0;

        StartCoroutine(LaunchAndAttachHook(hookIndex, distanceFromPoint));
    }

    void PlayHookFireSound(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.pitch = UnityEngine.Random.Range(0.85f, 1.25f);
        source.Play();
    }

   void PlayerJumpUpOnHookShot()
    {
        if (currentGasAmount > 0 && movementScript.IsGrounded == false)
        {
            movementScript.Rigidbody.AddForce(movementScript.Rigidbody.transform.up * gasDashForce / 2f, ForceMode.VelocityChange);
        }
        else
            return;
    }


    void CheckHookLocked(int hookIndex)
    {
        if (reelingInOutState[hookIndex] == 1)
        {
            isProperlyHooked = true;

        }

    }
    IEnumerator LaunchAndAttachHook(int hookIndex, float distanceToPoint)
    {
        PlayerJumpUpOnHookShot();


        if (!hookJoints[hookIndex])
        {
            StopHook(hookIndex);
            yield break;
        }

        Vector3 initialPosition = hookStartTransforms[hookIndex].position;
        Vector3 targetPosition = hookSwingPoints[hookIndex];
        float distance = Vector3.Distance(initialPosition, targetPosition);
        float travelTime = distance / hookEjectForce;

        float startTime = Time.time;
        while (true)
        {
            float elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / travelTime);
            Vector3 currentPosition = Vector3.Lerp(initialPosition, targetPosition, t);

            float currentDistance = Vector3.Distance(hookStartTransforms[hookIndex].position, hookSwingPoints[hookIndex]);
            if (currentDistance > hookMaxDistance)
            {
                StopHook(hookIndex);
                yield break;
            }

            hookPositions[hookIndex] = currentPosition;

            if (t >= 1f)
                break;

            yield return null;
        }

        reelingInOutState[hookIndex] = 1;
        
           
        if (hookJoints[hookIndex] && !hooksLatchAudioSources[hookIndex].isPlaying)
        {
            hooksReady[hookIndex] = false;
            hookJoints[hookIndex].maxDistance = distanceToPoint * 0.9f;
            hooksLatchAudioSources[hookIndex].gameObject.transform.position = hookSwingPoints[hookIndex];
            hooksLatchAudioSources[hookIndex].pitch = UnityEngine.Random.Range(0.8f, 1.2f);
            hooksLatchAudioSources[hookIndex].Play();

            hookJoints[hookIndex].spring = PL_ResourceManagement.MapToRange(movementScript.Rigidbody.velocity.sqrMagnitude, 1, 300, 7.5f, 20f);
            hookJoints[hookIndex].damper = PL_ResourceManagement.MapToRange(movementScript.Rigidbody.velocity.sqrMagnitude, 1, 50, 2.5f, 10f);
            hookJoints[hookIndex].massScale = PL_ResourceManagement.MapToRange(movementScript.Rigidbody.velocity.sqrMagnitude, 1, 50, 4.5f, 2f);

            hookSwingPoints[hookIndex] = hookSwingPoints[hookIndex];
            

        }
    }

    void StopHook(int hookIndex)
    {
        reelingInOutState[hookIndex] = 3;
        Destroy(hookJoints[hookIndex]);
    }

    public void KillPlayer()
    {
        StopHook(0);
        StopHook(1);

        gasAudioSource.Stop();
        gasDashAudioSource.Stop();
        hookFireAudioSources[0].Stop();
        hookFireAudioSources[1].Stop();
        hookReelAudioSources[0].Stop();
        hookReelAudioSources[1].Stop();
        hooksLatchAudioSources[0].Stop();
        hooksLatchAudioSources[1].Stop();
        gasParticles.Stop();
        gasDashParticles.Stop();

        this.enabled = false;
    }
}