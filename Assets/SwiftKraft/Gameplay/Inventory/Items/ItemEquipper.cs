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
        public EquippableItemType WishEquip
        {
            get => _wishEquip;
            set
            {
                if (_wishEquip == value)
                    return;

                _wishEquip = value;

                if (!unequipping)
                {
                    if (Current == null)
                        Equip();
                    else
                        Unequip();
                }
            }
        }
        [SerializeField]
        EquippableItemType _wishEquip;

        bool unequipping;

        private void Awake()
        {
            EquippedItemCache.AddRange(GetComponentsInChildren<EquippedItem>());

            ResetAll();
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
            unequipping = false;
        }

        public void Unequip()
        {
            if (Current != null)
            {
                Current.Unequip();
                unequipping = true;
            }
        }

        public void Equip()
        {
            EquippedItem eq = null;

            if (Current != null)
                Current.gameObject.SetActive(false);
            else if (WishEquip != null && (HasEquippedItem(WishEquip, out EquippedItem it) || AddEquippedItem(WishEquip, out it)))
                eq = it;

            if (eq != null)
                OnEquip?.Invoke(eq);

            Current = eq;
            unequipping = false;
        }
    }
}
