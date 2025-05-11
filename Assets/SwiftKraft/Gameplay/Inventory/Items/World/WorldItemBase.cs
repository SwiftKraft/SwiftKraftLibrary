using SwiftKraft.Gameplay.Interactions;
using SwiftKraft.Gameplay.Interfaces;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class WorldItemBase : MonoBehaviour, IInteractable
    {
        public ItemType StartType;

        public Guid Item { get; private set; }

        protected virtual void Awake()
        {
            if (StartType != null)
                Init(new ItemInstance(StartType).Guid);
        }

        public void Init(Guid item)
        {
            Item = item;

            foreach (WorldItemAddonBase addon in GetComponents<WorldItemAddonBase>())
                addon.Init(this);
        }

        public void Interact(InteractorBase interactor)
        {
            if (interactor.TryGetComponent(out InventoryBase inv))
            {
                inv.AddItem(Item);
                Destroy(gameObject);
            }
        }
    }
}
