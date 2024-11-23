using SwiftKraft.Gameplay.Common.FPS.ViewModels;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [RequireComponent(typeof(WeaponAmmoLoopedAnimator))]
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (ReloadCommunicator != null)
            {
                ReloadCommunicator.MidReload -= FinishCycle;
                ReloadCommunicator.EndReload -= EndReload;
            }
        }

        protected virtual void FinishCycle()
        {
            AddAmmo();
            if (Reloading)
                OnStartLoadEvent();
        }
    }
}
