using Newtonsoft.Json;
using SwiftKraft.Saving.Progress;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public static class ItemManager
    {
        public static readonly Dictionary<string, ItemType> Registered = new();
        public static Dictionary<Guid, ItemInstance> Instances => ItemScene.Instances;
        public static Data ItemScene = new();

        public static bool Register(this ItemType type) => Registered.TryAdd(type.ID, type);

        public static ItemType Get(string id) => TryGet(id, out ItemType type) ? type : null;
        public static bool TryGet(string id, out ItemType type) => Registered.TryGetValue(id, out type);

        public static bool AddInstance(this ItemInstance instance)
        {
            if (!Instances.ContainsKey(instance.Guid))
            {
                Instances.Add(instance.Guid, instance);
                return true;
            }
            return false;
        }

        public static void RemoveInstance(this ItemInstance instance) => Instances.Remove(instance.Guid);

        public static bool TryGetInstance(Guid id, out ItemInstance inst)
        {
            inst = GetInstance(id);
            return inst != null;
        }

        public static ItemInstance GetInstance(Guid id) => Instances.ContainsKey(id) ? Instances[id] : null;

        public class Data : Progressable
        {
            [JsonProperty]
            public Dictionary<Guid, ItemInstance> Instances = new();
        }
    }
}
