using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoLoopedAnimation : WeaponAmmoLooped
    {
        public string MidReloadAnimationID = "MidReload";
        public string EndReloadAnimationID = "EndReload";
        public string[] MidReloadStates = { "LoopReload" };
        public string[] EndReloadStates = { "EndReload" };
        public string[] StartReloadStates = { "StartReload" };
        public float FullMidReloadThreshold = 0.9f;
        public float FullEndReloadThreshold = 0.8f;
        public Animator Animator => WeaponAnimator.Animator.Animator;

        public WeaponAnimator WeaponAnimator
        {
            get
            {
                if (_weaponAnimator == null)
                    _weaponAnimator = GetComponentInChildren<WeaponAnimator>();

                return _weaponAnimator;
            }
        }
        WeaponAnimator _weaponAnimator;

        protected virtual void Update()
        {
            if (Animator.IsInTransition(0))
                return;

            AnimatorStateInfo info = Animator.GetCurrentAnimatorStateInfo(0);

            if (info.CheckName(MidReloadStates) && info.normalizedTime >= FullMidReloadThreshold)
            {
                AddAmmo();
                DecideEnd();
            }
            else if (info.CheckName(StartReloadStates) && info.normalizedTime >= 1f)
                DecideEnd();
            else if (info.CheckName(EndReloadStates) && info.normalizedTime >= FullEndReloadThreshold)
                EndReload(true);
        }

        public void DecideEnd()
        {
            if (Reloading)
                MidReload();
            else
                WeaponAnimator.PlayAnimation(EndReloadAnimationID);
        }

        protected override void Reload()
        {
            base.Reload();
            Animator.speed = ReloadSpeedMultiplier;
        }

        public override void MidReload()
        {
            base.MidReload();
            WeaponAnimator.PlayAnimation(MidReloadAnimationID);
        }
    }
}
