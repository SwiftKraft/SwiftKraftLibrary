using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmo : WeaponComponentBlocker
    {
        [field: SerializeField]
        public int MaxAmmo { get; set; }
        public int CurrentAmmo
        {
            get => _currentAmmo;
            set
            {
                if (_currentAmmo == value)
                    return;

                OnAmmoUpdated?.Invoke(value);
                _currentAmmo = value;
                AttackDisabler.Active = _currentAmmo <= 0;
            }
        }
        private int _currentAmmo;

        public readonly BooleanLock CanReload = new();

        public event Action<int> OnAmmoUpdated;

        protected override void Awake()
        {
            base.Awake();
            CurrentAmmo = MaxAmmo;
        }

        public bool HasAmmo(int ammo = 1) => CurrentAmmo >= ammo;

        public bool TryUseAmmo(int ammo, out int overused)
        {
            if (!HasAmmo())
            {
                overused = 0;
                return false;
            }

            CurrentAmmo -= ammo;
            overused = CurrentAmmo >= 0 ? 0 : Mathf.Abs(CurrentAmmo);

            if (CurrentAmmo < 0)
                CurrentAmmo = 0;

            return true;
        }

        public bool TryUseAmmo(int ammo = 1) => TryUseAmmo(ammo, out _);

        public void Reload()
        {
            if (CanReload)
                CurrentAmmo = MaxAmmo;
        }
    }
}
