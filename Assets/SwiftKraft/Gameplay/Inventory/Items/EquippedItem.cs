using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItem : MonoBehaviour
    {
        public EquippableItemType Item { get; private set; }

        public void Init(EquippableItemType type) => Item = type;
    }
}
