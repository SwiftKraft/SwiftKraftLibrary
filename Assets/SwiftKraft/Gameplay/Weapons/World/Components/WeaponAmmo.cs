using Newtonsoft.Json;
using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmo : WeaponComponentBlocker
    {
        public class Data : ItemDataBase
        {
            [JsonProperty]
            public int CurrentAmmo;
            [JsonProperty]
            public int ReserveAmmo;
        }

        public const string ReloadAction = "Reload";
        public const string AmmoSaveID = "Ammo";

        public EquippedItem Item { get; private set; }

        [field: SerializeField]
        public ModifiableStatistic MaxAmmo { get; set; }
        [field: SerializeField]
        public ModifiableStatistic ReloadSpeedMultiplier { get; set; }

        public int ReserveAmmo
        {
            get
            {
                if (Item == null || Item.Instance == null)
                    return _reserveAmmo;

                if (data == null || data.Disposed)
                    Item.Instance.TryData(AmmoSaveID, out data);

                return data.ReserveAmmo;
            }
            set
            {
                if (Item == null)
                {
                    OnAmmoUpdated?.Invoke(value);
                    _reserveAmmo = value;
                    return;
                }

                if (data == null || data.Disposed)
                    Item.Instance.TryData(AmmoSaveID, out data);

                OnAmmoUpdated?.Invoke(value);

                data.ReserveAmmo = value;
            }
        }
        int _reserveAmmo;

        public int CurrentAmmo
        {
            get
            {
                if (Item == null || Item.Instance == null)
                    return _currentAmmo;

                if (data == null || data.Disposed)
                    Item.Instance.TryData(AmmoSaveID, out data);

                return data.CurrentAmmo;
            }
            set
            {
                if (Item == null)
                {
                    OnAmmoUpdated?.Invoke(value);
                    _currentAmmo = value;
                    AttackDisabler.Active = _currentAmmo <= 0;
                    return;
                }

                if (data == null || data.Disposed)
                    Item.Instance.TryData(AmmoSaveID, out data);

                OnAmmoUpdated?.Invoke(value);
                
                data.CurrentAmmo = value;
                AttackDisabler.Active = data.CurrentAmmo <= 0;
            }
        }
        int _currentAmmo;

        public virtual bool Reloading => false;

        public readonly BooleanLock CanReload = new();

        public event Action<int> OnAmmoUpdated;
        public event Action<bool> OnReloadUpdated;
        protected void OnReloadUpdatedEvent(bool reload) => OnReloadUpdated?.Invoke(reload);

        protected BooleanLock.Lock CanShoot;

        protected Data data;

        protected override void Awake()
        {
            base.Awake();
            Item = GetComponent<EquippedItem>();
            if (Item != null)
                Item.OnEquip += OnEquip;
            else
                _currentAmmo = Mathf.RoundToInt(MaxAmmo);
            Parent.OnAttack += OnAttack;
            Parent.AddAction(ReloadAction, StartReload);
            CanShoot = Parent.CanAttack.AddLock();
            MaxAmmo.OnUpdate += OnMaxAmmoUpdated;
        }

        protected virtual void Start() => AttackDisabler.Active = CurrentAmmo <= 0;

        protected virtual void OnEnable() { }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Item != null)
                Item.OnEquip -= OnEquip;
            Parent.OnAttack -= OnAttack;
            Parent.Actions.Remove(ReloadAction);
            Parent.CanAttack.RemoveLock(CanShoot);
            MaxAmmo.OnUpdate -= OnMaxAmmoUpdated;
        }

        protected virtual void OnDisable()
        {
            CanShoot.Active = false;
            if (Reloading)
                EndReload(false);
        }

        protected virtual void OnEquip()
        {
            data = null;
            OnAmmoUpdated?.Invoke(CurrentAmmo);
            AttackDisabler.Active = CurrentAmmo <= 0;
        }

        protected virtual void OnMaxAmmoUpdated(float max) => CurrentAmmo = Mathf.Min(Mathf.RoundToInt(max), CurrentAmmo);

        protected virtual void OnAttack(GameObject[] go) => TryUseAmmo();

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

        protected virtual void Reload() { EndReload(true); }

        public virtual bool StartReload()
        {
            if (CanReload && CurrentAmmo < MaxAmmo && ReserveAmmo > 0 && !Reloading && !Parent.Attacking)
            {
                Reload();
                return true;
            }

            return false;
        }

        public virtual void EndReload(bool fullEnd)
        {
            if (CanReload && fullEnd)
            {
                int max = Mathf.RoundToInt(MaxAmmo);
                int exchange = max - CurrentAmmo;
                int actual = Mathf.Min(exchange, ReserveAmmo);
                CurrentAmmo += actual;
                ReserveAmmo -= actual;
            }
        }
    }
}
