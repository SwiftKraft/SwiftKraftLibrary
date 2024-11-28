using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [RequireComponent(typeof(EquippedItem))]
    public abstract class WeaponUnequipper : WeaponComponentBlocker // rework this to use items
    {
        public const string UnequipAction = "Unequip";

        public EquippedItem Item { get; private set; }

        protected BooleanLock.Lock CanUnequip;

        protected override void Awake()
        {
            base.Awake();
            Item = GetComponent<EquippedItem>();
            Item.OnUnequip += OnUnequip;
            CanUnequip = Item.CanUnequip.AddLock();
            Parent.AddAction(UnequipAction, StartUnequip);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Item.OnUnequip -= OnUnequip;
            Item.CanUnequip.RemoveLock(CanUnequip);
            Parent.Actions.Remove(UnequipAction);
        }

        protected virtual void OnUnequip()
        {
            if (!CanUnequip.Active && Item.CanUnequip)
                Parent.StartAction(UnequipAction);
        }

        public virtual bool StartUnequip()
        {
            AttackDisabler.Active = true;
            CanUnequip.Active = true;
            return true;
        }

        public virtual void EndUnequip()
        {
            AttackDisabler.Active = false;
            CanUnequip.Active = false;
            Item.FinishUnequip();
        }
    }
}