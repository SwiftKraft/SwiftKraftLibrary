using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoLoopedAnimation : WeaponAmmoLooped
    {
        public string[] MidReloadStates = { "LoopReload" };
        public string[] EndReloadStates = { "EndReload" };
        public string[] StartReloadStates = { "StartReload" };
        public float FullEndReloadThreshold = 0.9f;
        public Animator Animator
        {
            get
            {
                if (_animator == null)
                    _animator = GetComponentInChildren<Animator>();

                return _animator;
            }
        }
        Animator _animator;

        float reloadNormalizedTime;

        protected override void Awake()
        {
            base.Awake();
        }

        protected virtual void Update()
        {
            if (Animator.IsInTransition(0))
                return;

            if (Animator.GetCurrentAnimatorStateInfo(0).CheckName(MidReloadStates))
            {
                reloadNormalizedTime = Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                if (reloadNormalizedTime >= 1f)
                    EndReload(true);
            }
            else if (reloading)
            {
                EndReload(reloadNormalizedTime >= FullEndReloadThreshold);
                reloadNormalizedTime = 0f;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void Reload()
        {
            base.Reload();
            Animator.speed = ReloadSpeedMultiplier;
        }

        protected virtual void FinishCycle()
        {
            AddAmmo();
            if (Reloading)
                OnStartLoadEvent();
        }
    }
}
