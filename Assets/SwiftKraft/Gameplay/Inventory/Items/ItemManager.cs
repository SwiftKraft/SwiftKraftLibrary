using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public static class ItemManager
    {
        public static readonly Dictionary<string, ItemType> Registered = new();
        public static readonly HashSet<ItemInstance> Instances = new();

        public static bool Register(this ItemType type) => Registered.TryAdd(type.ID, type);

        public static ItemType Get(string id) => TryGet(id, out ItemType type) ? type : null;
        public static bool TryGet(string id, out ItemType type) => Registered.TryGetValue(id, out type);

        public static void AddInstance(this ItemInstance instance) => Instances.Add(instance);
        public static void RemoveInstance(this ItemInstance instance) => Instances.Remove(instance);
    }
}
