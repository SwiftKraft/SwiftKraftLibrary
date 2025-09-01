using SwiftKraft.Gameplay.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class ItemEquipper : MonoBehaviour, IItemEquipper
    {
        public readonly List<EquippedItemBase> EquippedItemCache = new();

        public Transform Workspace;

        public event Action<EquippedItemBase> OnEquip;

        public EquippedItemBase Current { get; private set; }
        ItemInstance tryEquip;

        private void Awake()
        {
            EquippedItemCache.AddRange(GetComponentsInChildren<EquippedItemBase>());

            ResetAll();
        }

        public bool TryEquip(ItemInstance inst, out EquippedItemBase it)
        {
            it = null;
            if (inst == null || inst.Type is not EquippableItemType ty || (!HasEquippedItem(ty, out it) && !AddEquippedItem(ty, out it)))
                return false;
            it.gameObject.SetActive(true);
            it.Equip(inst);
            return true;
        }

        public bool HasEquippedItem(EquippableItemType inst, out EquippedItemBase it)
        {
            foreach (EquippedItemBase item in EquippedItemCache)
                if (item.Item == inst)
                {
                    it = item;
                    return true;
                }
            it = null;
            return false;
        }

        public bool AddEquippedItem(EquippableItemType ty, out EquippedItemBase it)
        {
            if (ty == null || !ty.EquippedPrefab.TryGetComponent(out EquippedItemBase item))
            {
                it = null;
                return false;
            }

            it = Instantiate(item, Workspace);
            it.Init(this);

            EquippedItemCache.Add(it);

            return true;
        }

        public void ResetAll()
        {
            foreach (EquippedItemBase item in EquippedItemCache)
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
            else if (TryEquip(tryEquip, out EquippedItemBase eq))
            {
                if (eq != null)
                    OnEquip?.Invoke(eq);

                Current = eq;
            }
        }
    }
}
