using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmo : WeaponComponentBlocker
    {
        public const string ReloadAction = "Reload";

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

        public virtual bool Reloading => false;

        public readonly BooleanLock CanReload = new();

        public event Action<int> OnAmmoUpdated;

        protected BooleanLock.Lock CanShoot;

        protected override void Awake()
        {
            base.Awake();
            CurrentAmmo = MaxAmmo;
            Parent.OnAttack += OnAttack;
            Parent.AddAction(ReloadAction, StartReload);
            CanShoot = Parent.CanAttack.AddLock();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Parent.OnAttack -= OnAttack;
            Parent.Actions.Remove(ReloadAction);
            Parent.CanAttack.RemoveLock(CanShoot);
        }

        protected virtual void OnAttack() => TryUseAmmo();

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

        protected virtual void Reload() { EndReload(); }

        public virtual bool StartReload()
        {
            if (CanReload && !Reloading && !Parent.Attacking)
            {
                Reload();
                return true;
            }

            return false;
        }

        public virtual void EndReload()
        {
            if (CanReload)
                CurrentAmmo = MaxAmmo;
        }
    }
}