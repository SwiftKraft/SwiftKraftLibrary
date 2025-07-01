using SwiftKraft.Gameplay.Inventory.Items;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IItemEquipper
    {
        public EquippedItem Current { get; }
        public bool TryEquip(ItemInstance inst, out EquippedItem it);
        public void ForceUnequip(bool resetWishEquip = false);
        public void Equip(ItemInstance item);
    }
}
