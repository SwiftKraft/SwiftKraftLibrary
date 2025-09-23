using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.Characters
{
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonAnimator : MonoBehaviour
    {
        public Animator Animator { get; private set; }

        public AnimationClip Idle { get; set; }
        public AnimationClip Custom { get; set; }

        public AnimationClip OverrideAnimationIdle;
        public AnimationClip OverrideAnimationCustom;

        private void Awake() => Animator = GetComponent<Animator>();

        public void UpdateCustom(AnimationClip anim)
        {
            Custom = anim;
            UpdateCustom();
        }

        public void UpdateCustom()
        {
            AnimatorOverrideController cont = new(Animator.runtimeAnimatorController);
            List<KeyValuePair<AnimationClip, AnimationClip>> anims = new();

            foreach (AnimationClip a in cont.animationClips)
            {
                KeyValuePair<AnimationClip, AnimationClip> p =
                    a.name.Equals(OverrideAnimationCustom.name) ?
                        new(a, Custom) :
                        a.name.Equals(OverrideAnimationIdle.name) ?
                            new(a, Idle) :
                            new(a, a);

                anims.Add(p);
            }

            cont.ApplyOverrides(anims);
            Animator.runtimeAnimatorController = cont;
        }

        public void PlayCustom(AnimationClip anim, float transDur = 0.2f)
        {
            if (anim == null)
                return;

            UpdateCustom(anim);

            if (transDur >= 0f)
                Animator.CrossFadeInFixedTime("Custom", transDur, 1, 0f);
            else
                Animator.Play("Custom", 1, 0f);

            Animator.Update(0f);
        }
    }
}
