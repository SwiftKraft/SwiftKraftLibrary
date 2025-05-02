using System;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAmmoLooped : WeaponAmmo
    {
        public string[] CancelActions = { "Attack" };

        public int[] Amounts;

        public int NextAmount { get; private set; }

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

        public event Action<int> OnLoopedAmmoChanged;

        protected override void Awake()
        {
            base.Awake();
            Parent.OnAttemptAction += OnAttemptAction;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EndReload(true);
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

        protected override void Reload()
        {
            LoadedAmmo = 0;
            CanShoot.Active = true;
            UpdateNextAmount();
            OnReloadUpdatedEvent(true);
            reloading = true;
        }

        public override void EndReload(bool fullEnd)
        {
            CanShoot.Active = false;
            OnReloadUpdatedEvent(false);
            reloading = false;
        }

        public abstract void MidReload();

        public virtual void UpdateNextAmount()
        {
            foreach (int i in Amounts)
                if ((MaxAmmo - CurrentAmmo) >= i)
                {
                    NextAmount = i;
                    return;
                }
            NextAmount = Amounts.Length > 0 ? Amounts[0] : 1;
        }

        public virtual void AddAmmo()
        {
            if (CanReload)
            {
                UpdateNextAmount();
                CurrentAmmo = Mathf.Clamp(CurrentAmmo + NextAmount, 0, Mathf.RoundToInt(MaxAmmo));
                LoadedAmmo += NextAmount;

                if (CurrentAmmo >= MaxAmmo)
                {
                    OnReloadUpdatedEvent(false);
                    reloading = false;
                }
            }
        }
    }
}
