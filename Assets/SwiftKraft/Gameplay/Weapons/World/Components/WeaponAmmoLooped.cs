using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAmmoLooped : WeaponAmmo
    {
        public int Amount;

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

        protected void OnStartLoadEvent() => OnStartLoad?.Invoke();

        protected override void Reload() => LoadedAmmo = 0;

        public override void EndReload() => LoadedAmmo = 0;

        public virtual void AddAmmo()
        {
            if (CanReload)
            {
                CurrentAmmo = Mathf.Clamp(CurrentAmmo + Amount, 0, MaxAmmo);
                LoadedAmmo += Amount;
            }
        }
    }
}
