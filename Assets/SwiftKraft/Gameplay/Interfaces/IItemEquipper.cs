using SwiftKraft.Gameplay.Inventory.Items;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IItemEquipper
    {
        public EquippedItemBase Current { get; }
        public bool TryEquip(ItemInstance inst, out EquippedItemBase it);
        public void ForceUnequip(bool resetWishEquip = false);
        public void Equip(ItemInstance item);
    }
}
