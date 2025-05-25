using System;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAmmoLooped : WeaponAmmo
    {
        public string[] CancelActions = { "Attack" };

        public int[] Amounts;

        public int NextAmount
        {
            get => _nextAmount;
            private set
            {
                if (_nextAmount == value)
                    return;

                OnNextAmountChanged?.Invoke(value);
                _nextAmount = value;
            }
        }
        int _nextAmount;
        public event Action<int> OnNextAmountChanged;

        public override bool Reloading => reloading;
        protected bool reloading;

        public int LoadedAmmo
        {
            get => _loadedAmmo;
            private set
            {
                if (_loadedAmmo == value)
                    return;

                OnLoadedAmmoChanged?.Invoke(value);
                _loadedAmmo = value;
            }
        }
        int _loadedAmmo;
        public event Action<int> OnLoadedAmmoChanged;

        public bool MaxLoopedAmmoOnEmpty = true;

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
            LoadedAmmo = CurrentAmmo <= 0 ? Mathf.RoundToInt(MaxAmmo) : 0;
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

        public virtual void MidReload() => UpdateNextAmount();

        public virtual void UpdateNextAmount()
        {
            foreach (int i in Amounts)
                if (MaxAmmo - CurrentAmmo >= i)
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
