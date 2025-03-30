using SwiftKraft.Gameplay.Interactions;
using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class WorldItemBase : MonoBehaviour, IInteractable
    {
        public ItemType StartType;

        public ItemInstance Item { get; private set; }

        protected virtual void Awake()
        {
            if (StartType != null)
                Init(new ItemInstance(StartType));
        }

        public void Init(ItemInstance item) => Item = item;

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
