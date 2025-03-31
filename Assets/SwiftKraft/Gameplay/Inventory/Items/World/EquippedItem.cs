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

        public void Init(ItemEquipper parent) => Parent = parent;

        public event Action OnUnequip;
        public event Action OnEquip;

        public readonly BooleanLock CanUnequip = new();

        public void Equip(ItemInstance inst)
        {
            Instance = inst;
            OnEquip?.Invoke();
        }

        public void Unequip(bool resetWishEquip = false)
        {
            OnUnequip?.Invoke();

            if (!CanUnequip)
                return;

            FinishUnequip(resetWishEquip);
        }

        public void FinishUnequip(bool resetWishEquip = false) => Parent.ForceUnequip(resetWishEquip);
    }
}
