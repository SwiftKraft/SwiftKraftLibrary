using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItem : MonoBehaviour
    {
        public ItemEquipper Parent { get; private set; }

        public ItemInstance Instance { get; private set; }

        public EquippableItemType Item => Instance.Type is EquippableItemType eq ? eq : null;

        public void Init(ItemInstance inst, ItemEquipper parent)
        {
            Parent = parent;
            Instance = inst;
        }

        public event Action OnUnequip;
        public event Action OnEquip;

        public readonly BooleanLock CanUnequip = new();

        public void Equip() => OnEquip?.Invoke();

        public void Unequip()
        {
            OnUnequip?.Invoke();

            if (!CanUnequip)
                return;

            FinishUnequip();
        }

        public void FinishUnequip() => Parent.ForceUnequip();
    }
}
