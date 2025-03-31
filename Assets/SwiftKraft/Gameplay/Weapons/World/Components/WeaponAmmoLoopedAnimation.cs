using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoLoopedAnimation : WeaponAmmoLooped
    {
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

        public WeaponReloadAnimator ReloadCommunicator
        {
            get
            {
                if (_reloadCommunicator == null)
                    _reloadCommunicator = Animator.GetBehaviour<WeaponReloadAnimator>();

                return _reloadCommunicator;
            }
        }
        WeaponReloadAnimator _reloadCommunicator;

        protected override void Awake()
        {
            base.Awake();
            ReloadCommunicator.MidReload += FinishCycle;
            ReloadCommunicator.EndReload += EndReload;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ReloadCommunicator.MidReload -= FinishCycle;
            ReloadCommunicator.EndReload -= EndReload;
            ReloadCommunicator.MidReload += FinishCycle;
            ReloadCommunicator.EndReload += EndReload;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (ReloadCommunicator != null)
            {
                ReloadCommunicator.MidReload -= FinishCycle;
                ReloadCommunicator.EndReload -= EndReload;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (ReloadCommunicator != null)
            {
                ReloadCommunicator.MidReload -= FinishCycle;
                ReloadCommunicator.EndReload -= EndReload;
            }
        }

        protected override void Reload()
        {
            base.Reload();
            ReloadCommunicator.ReloadSpeed = ReloadSpeedMultiplier;
        }

        protected virtual void FinishCycle()
        {
            AddAmmo();
            if (Reloading)
                OnStartLoadEvent();
        }
    }
}
