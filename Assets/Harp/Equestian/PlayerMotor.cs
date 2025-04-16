using SwiftKraft.Utils;
using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Player.Movement
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        public delegate void OnPlayerGroundedChanged(bool curr, bool prev);

        public PlayerMovementStateBase DefaultState;

        public PlayerMovementStateBase SlideState;

        public PlayerMovementStateBase CurrentState
        {
            get
            {
                if (state == null)
                {
                    state = DefaultState;
                    state.StateStarted(this);
                }

                return state;
            }
            set
            {
                if (state == value || (value == null && state == DefaultState))
                    return;

                if (state != null)
                    state.StateEnded(this);
                state = value != null ? value : DefaultState;
                state.StateStarted(this);
            }
        }

        public Rigidbody Rigidbody
        {
            get;
            private set;
        }

        public CapsuleCollider Collider
        {
            get;
            private set;
        }

        public bool IsGrounded
        {
            get => isGrounded;
            protected set
            {
                if (isGrounded == value)
                    return;
                bool prev = isGrounded;
                isGrounded = value;
                CurrentState.GroundedChanged(this, value, prev);
                OnGroundedChanged?.Invoke(value, prev);
            }
        }

        public float Height
        {
            get => Collider.height;
            set
            {
                Collider.height = value;
                Collider.center = new(0f, value / 2f, 0f);
            }
        }

        public float CameraHeight
        {
            get => CameraRoot.localPosition.y;
            protected set
            {
                CameraRoot.localPosition = new(0f, value, 0f);
            }
        }

        public float ViableHeight
        {
            get
            {
                if (Physics.Raycast(GroundPoint.position + (Vector3.up * 0.01f), Vector3.up, out RaycastHit _hit, Mathf.Infinity, GroundLayers, QueryTriggerInteraction.Ignore))
                    return _hit.distance;
                return Mathf.Infinity;
            }
        }

        public Timer RecentJumpTimer;

        public GameObject GroundObject { get; private set; }

        [HideInInspector]
        public float TargetCameraHeight;

        [HideInInspector]
        public float slideBoostTimer;

        public float OriginalCameraHeight = 1.7f;
        public float CameraSmoothTime = 0.1f;

        public OnPlayerGroundedChanged OnGroundedChanged;

        public Transform CameraRoot;
        public Transform GroundPoint;

        public float GroundRadius;

        public LayerMask GroundLayers;

        public AudioSource[] Audio;
        public ParticleSystem[] Particles;

        public float currentSpeed;
        PlayerMovementStateBase state;
        bool isGrounded;
        float cameraVel;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Collider = GetComponent<CapsuleCollider>();

           

            TargetCameraHeight = OriginalCameraHeight;

            if (DefaultState == null)
                enabled = false;

            
            
        }
        private void Start()
        {
            StartCoroutine(UpdateSpeed());
        }


        private IEnumerator UpdateSpeed()
        {
            while (true) // This will run indefinitely
            {
                currentSpeed = Mathf.Ceil(Rigidbody.velocity.magnitude);
                yield return new WaitForSeconds(1f); // Wait for 1 second
            }
        }

      

        private void Update()
        {
            CameraHeight = Mathf.SmoothDamp(CameraHeight, TargetCameraHeight, ref cameraVel, CameraSmoothTime);


            CurrentState.InputUpdate(this);
        }

        private void FixedUpdate()
        {
            RecentJumpTimer.Tick(Time.fixedDeltaTime);

            Collider[] cols = Physics.OverlapSphere(GroundPoint.position, GroundRadius, GroundLayers).OrderBy(c => (c.transform.position - c.transform.position).sqrMagnitude).ToArray();
            IsGrounded = cols.Length > 0;
            GroundObject = IsGrounded ? cols[0].gameObject : null;

            if (slideBoostTimer > 0f)
                slideBoostTimer -= Time.fixedDeltaTime;
            else if (slideBoostTimer < 0f)
                slideBoostTimer = 0f;

            CurrentState.TickUpdate(this);
        }

        public Vector3 GetWishDir() => (Input.GetAxisRaw("Horizontal") * transform.right + Input.GetAxisRaw("Vertical") * transform.forward).normalized;
        public Vector3 GetGroundNormal()
        {
            if (IsGrounded && Physics.Raycast(GroundPoint.position + Vector3.up, Vector3.down, out RaycastHit _hit, 2f, GroundLayers, QueryTriggerInteraction.Ignore))
                return _hit.normal;
            return Vector3.zero;
        }

        public bool GetWishJump() => Input.GetKeyDown(KeyCode.Space);
        public void PlayMotorSound(int index)
        {
            if (TryGetSound(index, out AudioSource au)) 
                au.Play();
        }

        public AudioSource GetSound(int index)
        {
            if (index >= Audio.Length || index < 0)
                return null;

            return Audio[index];
        }

        public bool TryGetSound(int index, out AudioSource au)
        {
            au = GetSound(index);
            return au != null;
        }

        public void PlayMotorParticle(int index)
        {
            if (TryGetParticle(index, out ParticleSystem particle))
                particle.Play();
        }

        public ParticleSystem GetParticle(int index)
        {
            if (index >= Particles.Length || index < 0)
                return null;

            return Particles[index];
        }

        public bool TryGetParticle(int index, out ParticleSystem au)
        {
            au = GetParticle(index);
            return au != null;
        }
    }
}
