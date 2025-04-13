using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Map
{
    public class DoorAnimator : DoorBase
    {
        public Animator Animator { get; private set; }

        public string BoolName = "IsOpen";

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
                Animator.SetBoolSafe(BoolName, isOpen);
                Animator.Update(0f);
            }
        }
    }
}
