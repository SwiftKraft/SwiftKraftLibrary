using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAnimationChangeProperty : AttachmentComponentPropertyBase<WeaponAnimator>
    {
        public Override[] Overrides;

        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone()
            => new AttachmentAnimationChangeProperty()
            {
                Overrides = (Override[])Overrides.Clone(),
            };

        public override void Update()
        {
            for (int i = 0; i < Overrides.Length; i++)
                Component.Animator.SwapAnimation(Overrides[i].OriginalClip, Overrides[i].OverrideClip);
        }

        public override void Uninstall()
        {
            base.Uninstall();
            for (int i = 0; i < Overrides.Length; i++)
                Component.Animator.ResetAnimation(Overrides[i].OriginalClip);
        }

        [Serializable]
        public struct Override
        {
            public AnimationClip OriginalClip;
            public AnimationClip OverrideClip;

            public Override(AnimationClip original, AnimationClip overr)
            {
                OriginalClip = original;
                OverrideClip = overr;
            }
        }
    }
}