using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [RequireComponent(typeof(EquippedItem))]
    public abstract class WeaponEquipper : WeaponComponentBlocker // rework this to use items
    {
        public const string EquipAction = "Equip";

        public EquippedItem Item { get; private set; }

        public event Action OnStartEquip;
        public event Action OnEndEquip;

        protected BooleanLock.Lock CanUnequip;

        protected override void Awake()
        {
            base.Awake();
            Item = GetComponent<EquippedItem>();
            Item.OnEquip += Initialize;
            CanUnequip = Item.CanUnequip.AddLock();
            CanUnequip.Active = true;
            Parent.AddAction(EquipAction, StartEquip);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Item.OnEquip -= Initialize;
            Item.CanUnequip.RemoveLock(CanUnequip);
            Parent.Actions.Remove(EquipAction);
        }

        public virtual void Initialize()
        {
            CanUnequip.Active = true;
            Parent.StartAction(EquipAction);
        }

        public virtual bool StartEquip()
        {
            AttackDisabler.Active = true;
            CanUnequip.Active = true;
            OnStartEquip?.Invoke();
            return true;
        }

        public virtual void EndEquip()
        {
            AttackDisabler.Active = false;
            CanUnequip.Active = false;
            OnEndEquip?.Invoke();
        }
    }
}
