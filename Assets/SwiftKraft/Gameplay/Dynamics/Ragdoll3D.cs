using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Dynamics
{
    public class Ragdoll3D : RagdollBase
    {
        [field: SerializeField]
        public int RagdollLayer { get; private set; }
        [field: SerializeField]
        public int OriginalLayer { get; private set; }

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

        protected Animator animator;

        protected virtual void Awake()
        {
            Rigidbodies = GetComponentsInChildren<Rigidbody>();
            animator = GetComponent<Animator>();
            PreRagdollData = new TransformData[Rigidbodies.Length];
            State = false;
        }

        public override void Ragdoll(bool state)
        {
            if (animator != null)
                animator.enabled = !state;

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
        }

        public void RecordTransform()
        {
            for (int i = 0; i < PreRagdollData.Length; i++)
                PreRagdollData[i] = new(Rigidbodies[i].transform.localPosition, Rigidbodies[i].transform.localRotation);
        }

        public void ResetTransform()
        {
            for (int i = 0; i < Rigidbodies.Length; i++)
                Rigidbodies[i].transform.SetLocalPositionAndRotation(PreRagdollData[i].Position, PreRagdollData[i].Rotation);
        }
    }
}
