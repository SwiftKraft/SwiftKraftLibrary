using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItem : MonoBehaviour
    {
        public ItemEquipper Parent { get; private set; }

        public EquippableItemType Item { get; private set; }

        public void Init(EquippableItemType type, ItemEquipper parent)
        {
            Parent = parent;
            Item = type;
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
