using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [RequireComponent(typeof(WorldItemBase))]
    public abstract class WorldItemAddonBase : MonoBehaviour
    {
        public WorldItemBase Parent { get; private set; }

        public ItemInstance Item => Parent != null ? Parent.ItemSerial : null;

        public virtual void Init(WorldItemBase parent) => Parent = parent;
    }
}
