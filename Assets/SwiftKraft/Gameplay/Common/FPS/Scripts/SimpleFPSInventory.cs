using SwiftKraft.Gameplay.Inventory;
using SwiftKraft.Gameplay.Inventory.Items;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    public class SimpleFPSInventory : InventoryBase
    {
        public EquippableItemType[] DefaultItems;

        public ItemEquipper Equipper { get; private set; }

        public SlotSelector[] Selectors;

        private void Awake()
        {
            Equipper = GetComponentInChildren<ItemEquipper>();

            foreach (EquippableItemType it in DefaultItems)
                AddItem(new(it));
        }

        private void Update()
        {
            foreach (SlotSelector sel in Selectors)
                if (Input.GetKeyDown(sel.Key) && Data.Items.Count > sel.Slot && Data.Items[sel.Slot] != null && Data.Items[sel.Slot].Type is EquippableItemType ty)
                    Equipper.Equip(ty);
        }

        [Serializable]
        public class SlotSelector
        {
            public KeyCode Key;
            public int Slot;
        }
    }
}
