using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class ItemEquipper : MonoBehaviour
    {
        public readonly List<EquippedItem> EquippedItemCache = new();

        public Transform Workspace;

        public event Action<EquippedItem> OnEquip;

        public EquippedItem Current { get; private set; }
        EquippableItemType tryEquip;

        private void Awake()
        {
            EquippedItemCache.AddRange(GetComponentsInChildren<EquippedItem>());

            ResetAll();
        }

        public bool TryEquip(EquippableItemType type, out EquippedItem it)
        {
            if (!HasEquippedItem(type, out it) && !AddEquippedItem(type, out it))
                return false;
            it.gameObject.SetActive(true);
            return true;
        }

        public bool HasEquippedItem(EquippableItemType type, out EquippedItem it)
        {
            foreach (EquippedItem item in EquippedItemCache)
                if (item.Item == type)
                {
                    it = item;
                    return true;
                }
            it = null;
            return false;
        }

        public bool AddEquippedItem(EquippableItemType prefab, out EquippedItem it)
        {
            if (!prefab.EquippedPrefab.TryGetComponent(out EquippedItem item))
            {
                it = null;
                return false;
            }

            it = Instantiate(item, Workspace);
            it.Init(prefab, this);

            EquippedItemCache.Add(it);

            return true;
        }

        public void ResetAll()
        {
            foreach (EquippedItem item in EquippedItemCache)
                item.gameObject.SetActive(false);
        }

        public void ForceUnequip()
        {
            ResetAll();
            Current = null;
            UpdateEquip();
        }

        public void Equip(EquippableItemType item)
        {
            tryEquip = item;
            UpdateEquip();
        }

        protected void UpdateEquip()
        {
            if (Current != null)
            {
                if (Current.CanUnequip)
                    ForceUnequip();
                else
                    Current.Unequip();
            }
            else if (TryEquip(tryEquip, out EquippedItem eq))
            {
                if (eq != null)
                    OnEquip?.Invoke(eq);

                Current = eq;
            }
        }
    }
}
