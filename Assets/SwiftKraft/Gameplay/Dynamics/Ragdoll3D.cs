using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Dynamics
{
    public class Ragdoll3D : RagdollBase
    {
        [field: Header("Layers")]
        [field: SerializeField]
        public int RagdollLayer { get; private set; }
        [field: SerializeField]
        public int OriginalLayer { get; private set; }

        public float ColliderMultiplier = 0.5f;

        public override bool State
        {
            get => _state;
            set
            {
                if (value)
                    RecordTransform();
                else
                    ResetTransform();

                _state = value;
                Ragdoll(_state);
            }
        }
        private bool _state;

        public Rigidbody[] Rigidbodies { get; protected set; }
        public TransformData[] PreRagdollData { get; protected set; }

        protected CapsuleCollider[] capsules;
        protected SphereCollider[] spheres;
        protected BoxCollider[] boxes;

        protected Animator animator;

        bool initializedRagdollData;
        bool collidersSet;

        protected virtual void Awake()
        {
            Rigidbodies = GetComponentsInChildren<Rigidbody>();
            animator = GetComponent<Animator>();
            PreRagdollData = new TransformData[Rigidbodies.Length];
            State = false;

            capsules = GetComponentsInChildren<CapsuleCollider>();
            spheres = GetComponentsInChildren<SphereCollider>();
            boxes = GetComponentsInChildren<BoxCollider>();

            collidersSet = true;
        }

        public void MultiplyColliderSize(float amount)
        {
            for (int i = 0; i < capsules.Length; i++)
            {
                capsules[i].radius *= amount;
                capsules[i].height *= amount;
            }

            for (int i = 0; i < spheres.Length; i++)
                spheres[i].radius *= amount;

            for (int i = 0; i < boxes.Length; i++)
                boxes[i].size *= amount;
        }

        public override void Ragdoll(bool state)
        {
            if (animator != null)
                animator.enabled = !state;

            if (collidersSet)
                MultiplyColliderSize(state ? ColliderMultiplier : 1f / ColliderMultiplier);

            foreach (Rigidbody r in Rigidbodies)
            {
                r.isKinematic = !state;
                r.useGravity = state;
                r.solverIterations = state ? Physics.defaultSolverIterations : 1;

                if (RagdollLayer != OriginalLayer)
                    r.gameObject.layer = state ? RagdollLayer : OriginalLayer;

                if (!state)
                    r.Sleep();
                else
                    r.WakeUp();
            }

            if (state && DestroyAfter)
                Destroy(DestroyTarget != null ? DestroyTarget : gameObject, DestroyTime);
        }

        public void RecordTransform()
        {
            for (int i = 0; i < PreRagdollData.Length; i++)
                PreRagdollData[i] = new(Rigidbodies[i].transform.localPosition, Rigidbodies[i].transform.localRotation);
        }

        public void ResetTransform()
        {
            if (!initializedRagdollData)
            {
                initializedRagdollData = true;
                RecordTransform();
                return;
            }

            for (int i = 0; i < Rigidbodies.Length; i++)
                Rigidbodies[i].transform.SetLocalPositionAndRotation(PreRagdollData[i].Position, PreRagdollData[i].Rotation);
        }
    }
}
