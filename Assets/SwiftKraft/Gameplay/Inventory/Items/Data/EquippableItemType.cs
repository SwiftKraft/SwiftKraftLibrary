using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Inventory/Item/Equippable", fileName = "Equippable", order = 1)]
    public class EquippableItemType : ItemType
    {
        public GameObject EquippedPrefab;
    }
}
