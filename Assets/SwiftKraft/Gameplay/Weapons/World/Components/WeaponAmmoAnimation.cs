using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoAnimation : WeaponAmmo
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

        public override bool Reloading => reloading;
        bool reloading;

        protected override void Awake()
        {
            base.Awake();

            ReloadCommunicator.EndReload += EndReload;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (ReloadCommunicator != null)
                ReloadCommunicator.EndReload -= EndReload;
        }

        protected override void Reload()
        {
            CanShoot.Active = true;
            OnReloadUpdatedEvent(true);
            reloading = true;
        }

        public override void EndReload(bool fullEnd)
        {
            base.EndReload(fullEnd);
            CanShoot.Active = false;
            OnReloadUpdatedEvent(false);
            reloading = false;
        }
    }
}
