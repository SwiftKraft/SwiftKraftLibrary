using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Animation
{
    public abstract class AnimatorFloatSetterBase : MonoBehaviour
    {
        [Tooltip("Leave as null if the GameObject contains an Animator.")]
        public Animator Animator;

        public string VariableName;

        public Interpolater Interpolater { get; protected set; }

        protected virtual void Awake()
        {
            if (Animator == null)
                Animator = GetComponent<Animator>();

            Interpolater = AssignInterpolater();
        }

        protected virtual void Update()
        {
            if (Animator == null)
                return;

            if (Interpolater == null)
            {
                Animator.SetFloatSafe(VariableName, GetMaxValue());
                return;
            }

            Interpolater.Tick(Time.deltaTime);
            Interpolater.MaxValue = GetMaxValue();

            Animator.SetFloatSafe(VariableName, Interpolater.CurrentValue);
        }

        public abstract float GetMaxValue();

        public abstract Interpolater AssignInterpolater();
    }
}
