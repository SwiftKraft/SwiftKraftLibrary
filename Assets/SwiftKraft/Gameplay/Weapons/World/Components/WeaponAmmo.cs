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
        }

        public const string ReloadAction = "Reload";
        public const string AmmoSaveID = "Ammo";

        public EquippedItem Item { get; private set; }

        [field: SerializeField]
        public ModifiableStatistic MaxAmmo { get; set; }
        [field: SerializeField]
        public ModifiableStatistic ReloadSpeedMultiplier { get; set; }

        public int CurrentAmmo
        {
            get
            {
                if (Item == null)
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
                    AttackDisabler.Active = data.CurrentAmmo <= 0;
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
            Parent.OnAttack += OnAttack;
            Parent.AddAction(ReloadAction, StartReload);
            CanShoot = Parent.CanAttack.AddLock();
            MaxAmmo.OnUpdate += OnMaxAmmoUpdated;
        }

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
            Debug.Log(CurrentAmmo);
        }

        protected virtual void OnMaxAmmoUpdated(float max)
        {
            CurrentAmmo = Mathf.Min(Mathf.RoundToInt(max), CurrentAmmo);
            OnAmmoUpdated?.Invoke(CurrentAmmo);
        }

        protected virtual void OnAttack(GameObject go) => TryUseAmmo();

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
            if (CanReload && CurrentAmmo < MaxAmmo && !Reloading && !Parent.Attacking)
            {
                Reload();
                return true;
            }

            return false;
        }

        public virtual void EndReload(bool fullEnd)
        {
            if (CanReload && fullEnd)
                CurrentAmmo = Mathf.RoundToInt(MaxAmmo);
            Debug.Log("End reload");
        }
    }
}
