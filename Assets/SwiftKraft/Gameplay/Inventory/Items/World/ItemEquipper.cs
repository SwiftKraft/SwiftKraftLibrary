using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Gameplay.Interfaces;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class ItemEquipper : PetBehaviourBase, IItemEquipper
    {
        public readonly List<EquippedItemBase> EquippedItemCache = new();

        public Transform Workspace;

        public event Action<EquippedItemBase> OnEquip;

        public EquippedItemBase Current { get; private set; }
        public ItemInstance WishEquip { get; set; }

        protected virtual void Awake()
        {
            Owner = GetComponent<IPawn>();
            EquippedItemCache.AddRange(GetComponentsInChildren<EquippedItemBase>());
            ResetAll();
        }

        protected virtual void FixedUpdate()
        {
            if (Current == null)
            {
                if (WishEquip == null || WishEquip.Disposed)
                    return;

                if (TryEquip(WishEquip, out EquippedItemBase b))
                {
                    Current = b;
                    OnEquip?.Invoke(Current);
                    return;
                }
            }

            if ((WishEquip != Current.Instance || Current.Instance.Disposed) && Current.AttemptUnequip())
                ForceUnequip();
        }

        public bool TryEquip(ItemInstance inst, out EquippedItemBase it)
        {
            it = null;
            if (inst == null || inst.Type is not EquippableItemType ty || (!HasEquippedItem(ty, out it) && !AddEquippedItem(ty, out it)))
                return false;
            ResetAll();
            it.gameObject.SetActive(true);
            it.Equip(inst);
            return true;
        }

        public bool HasEquippedItem(EquippableItemType inst, out EquippedItemBase it)
        {
            for (int i = 0; i < EquippedItemCache.Count; i++)
                if (EquippedItemCache[i].Item == inst)
                {
                    it = EquippedItemCache[i];
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
            for (int i = 0; i < EquippedItemCache.Count; i++)
                EquippedItemCache[i].gameObject.SetActive(false);
        }

        public void ForceUnequip(bool resetWishEquip = false)
        {
            if (resetWishEquip)
                WishEquip = null;

            if (Current != null)
                Current.Unequip();

            ResetAll();
            Current = null;
        }

        public void Equip(ItemInstance item)
        {
            if (Current != null && Current.Instance == item)
                return;

            WishEquip = item;
        }
    }
}
