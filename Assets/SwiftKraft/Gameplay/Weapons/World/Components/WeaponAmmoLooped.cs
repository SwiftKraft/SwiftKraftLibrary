using System;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAmmoLooped : WeaponAmmo
    {
        public string[] CancelActions = { "Attack" };

        public int Amount;

        public override bool Reloading => reloading;
        protected bool reloading;

        public int LoadedAmmo
        {
            get => _loadedAmmo;
            private set
            {
                if (_loadedAmmo == value)
                    return;

                OnLoopedAmmoChanged?.Invoke(value);
                _loadedAmmo = value;
            }
        }
        int _loadedAmmo;

        public event Action OnStartLoad;
        public event Action<int> OnLoopedAmmoChanged;

        protected override void Awake()
        {
            base.Awake();
            Parent.OnAttemptAction += OnAttemptAction;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Parent.OnAttemptAction -= OnAttemptAction;
        }

        protected virtual void OnAttemptAction(string obj)
        {
            if (CancelActions.Contains(obj))
            {
                OnReloadUpdatedEvent(false);
                reloading = false;
            }
        }

        protected void OnStartLoadEvent() => OnStartLoad?.Invoke();

        protected override void Reload()
        {
            LoadedAmmo = 0;
            CanShoot.Active = true;
            OnReloadUpdatedEvent(true);
            reloading = true;
        }

        public override void EndReload(bool fullEnd)
        {
            LoadedAmmo = 0;
            if (fullEnd)
            {
                CanShoot.Active = false;
                OnReloadUpdatedEvent(false);
                reloading = false;
            }
        }

        public virtual void AddAmmo()
        {
            if (CanReload)
            {
                CurrentAmmo = Mathf.Clamp(CurrentAmmo + Amount, 0, MaxAmmo);
                LoadedAmmo += Amount;

                if (CurrentAmmo >= MaxAmmo)
                {
                    OnReloadUpdatedEvent(false);
                    reloading = false;
                }
            }
        }
    }
}
