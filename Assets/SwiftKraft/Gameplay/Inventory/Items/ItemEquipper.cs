using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class ItemEquipper : MonoBehaviour
    {
        public readonly List<EquippedItem> EquippedItemCache = new();

        public Transform Workspace;

        public event Action<EquippedItem> OnEquip;

        public EquippedItem Current { get; private set; }
        ItemInstance tryEquip;

        private void Awake()
        {
            EquippedItemCache.AddRange(GetComponentsInChildren<EquippedItem>());

            ResetAll();
        }

        public bool TryEquip(ItemInstance inst, out EquippedItem it)
        {
            if (!HasEquippedItem(inst, out it) && !AddEquippedItem(inst, out it))
                return false;
            it.gameObject.SetActive(true);
            it.Equip();
            return true;
        }

        public bool HasEquippedItem(ItemInstance inst, out EquippedItem it)
        {
            foreach (EquippedItem item in EquippedItemCache)
                if (item.Instance == inst)
                {
                    it = item;
                    return true;
                }
            it = null;
            return false;
        }

        public bool AddEquippedItem(ItemInstance inst, out EquippedItem it)
        {
            if (inst == null || inst.Type is not EquippableItemType ty || !ty.EquippedPrefab.TryGetComponent(out EquippedItem item))
            {
                it = null;
                return false;
            }

            it = Instantiate(item, Workspace);
            it.Init(inst, this);

            EquippedItemCache.Add(it);

            return true;
        }

        public void ResetAll()
        {
            foreach (EquippedItem item in EquippedItemCache)
                item.gameObject.SetActive(false);
        }

        public void ForceUnequip(bool resetWishEquip = false)
        {
            ResetAll();
            Current = null;
            if (resetWishEquip)
                tryEquip = null;
            UpdateEquip();
        }

        public void Equip(ItemInstance item)
        {
            if (Current != null && Current.Instance == item)
                return;

            tryEquip = item;
            UpdateEquip();
        }

        protected void UpdateEquip()
        {
            if (Current != null)
                Current.Unequip();
            else if (TryEquip(tryEquip, out EquippedItem eq))
            {
                if (eq != null)
                    OnEquip?.Invoke(eq);

                Current = eq;
            }
        }
    }
}
