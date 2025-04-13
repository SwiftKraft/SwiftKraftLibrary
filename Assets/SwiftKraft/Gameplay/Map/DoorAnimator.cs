using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Map
{
    public class DoorAnimator : DoorBase
    {
        public Animator Animator { get; private set; }

        public float CrossFadeTime = 0.1f;
        public string OpenName = "Open";
        public string CloseName = "Close";

        protected virtual void Awake()
        {
            if (TryGetComponent(out Animator anim) || this.TryGetComponentInChildren(out anim))
                Animator = anim;
        }

        protected virtual void Start()
        {
            if (Animator != null)
                Animator.Play(Animator.GetNextAnimatorStateInfo(0).shortNameHash, 0, 1f);
        }

        protected virtual void Update()
        {
            if (Animator != null && !Animator.IsInTransition(0))
                Status = Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        public override void SetState(bool isOpen)
        {
            IsOpen = isOpen;
            if (Animator != null)
            {
                string state = isOpen ? OpenName : CloseName;
                if (CrossFadeTime > 0f)
                    Animator.CrossFadeInFixedTime(state, CrossFadeTime, 0, 0f);
                else
                    Animator.Play(state, 0, 0f);
                Animator.Update(0f);
            }
        }
    }
}
