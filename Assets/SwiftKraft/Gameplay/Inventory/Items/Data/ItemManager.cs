using Newtonsoft.Json;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public static class ItemManager
    {
        public static readonly Dictionary<string, ItemType> Registered = new();
        public static readonly SerialGenerator SerialGenerator = new();

        public static Dictionary<uint, ItemInstance> Instances => CurrentScene.Instances;
        public static ItemScene CurrentScene 
        {
            get
            {
                _currentScene ??= new();
                return _currentScene;
            }

            set
            {
                if (value == null)
                {
                    _currentScene = new();
                    return;
                }

                _currentScene = value;
                _currentScene.Refresh();
            }
        }
        static ItemScene _currentScene;

        public static bool Register(this ItemType type) => Registered.TryAdd(type.ID, type);

        public static ItemType GetRegistered(string id) => TryGetRegistered(id, out ItemType type) ? type : null;
        public static bool TryGetRegistered(string id, out ItemType type) => Registered.TryGetValue(id, out type);

        public static bool AddInstance(this ItemInstance instance)
        {
            if (!Instances.ContainsKey(instance.Serial))
            {
                Instances.Add(instance.Serial, instance);
                return true;
            }
            return false;
        }

        public static void RemoveInstance(uint id) => Instances.Remove(id);

        public static void RemoveInstance(this ItemInstance instance) => RemoveInstance(instance.Serial);

        public static bool TryGetInstance(uint id, out ItemInstance inst)
        {
            inst = GetInstance(id);
            return inst != null;
        }

        public static ItemInstance GetInstance(uint id) => Instances.ContainsKey(id) ? Instances[id] : null;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ItemScene
    {
        [JsonProperty]
        public Dictionary<uint, ItemInstance> Instances = new();

        public void Refresh()
        {
            foreach (ItemInstance inst in Instances.Values)
                inst.Refresh();
        }
    }
}
