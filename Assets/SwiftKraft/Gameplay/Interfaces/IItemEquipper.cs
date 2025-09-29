using SwiftKraft.Gameplay.Inventory.Items;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IItemEquipper : IPet
    {
        public EquippedItemBase Current { get; }
        public ItemInstance WishEquip { get; set; }
        public bool TryEquip(ItemInstance inst, out EquippedItemBase it);
        public void ForceUnequip(bool resetWishEquip = false);
        public void Equip(ItemInstance item);
    }
}
