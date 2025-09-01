using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItemBase : MonoBehaviour
    {
        public ItemEquipper Parent { get; private set; }

        public ItemInstance Instance { get; private set; }

        public EquippableItemType Item => Instance.Type is EquippableItemType eq ? eq : null;

        public void Init(ItemEquipper parent) => Parent = parent;

        public event Action OnUnequip;
        public event Action OnEquip;

        public readonly BooleanLock CanUnequip = new();

        public virtual void Equip(ItemInstance inst)
        {
            Instance = inst;
            OnEquip?.Invoke();
        }

        public virtual void Unequip() => OnUnequip?.Invoke();

        /// <summary>
        /// Runs every tick where it tries to unequip; if it returns <b>true</b>, it will unequip.
        /// </summary>
        /// <returns>Whether the unequip is allowed.</returns>
        public virtual bool AttemptUnequip() => true;
    }
}
