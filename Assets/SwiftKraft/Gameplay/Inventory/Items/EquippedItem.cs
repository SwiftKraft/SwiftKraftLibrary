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

        public event Action OnFailedUnequip;

        public readonly BooleanLock CanUnequip = new();

        public void Unequip()
        {
            if (!CanUnequip)
            {
                OnFailedUnequip?.Invoke();
                return;
            }

            Parent.Equip();
        }
    }
}
