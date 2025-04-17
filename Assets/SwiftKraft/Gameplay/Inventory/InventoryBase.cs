using Newtonsoft.Json;
using SwiftKraft.Gameplay.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory
{
    public abstract class InventoryBase : MonoBehaviour
    {
        public virtual InventoryInstance Data
        {
            get
            {
                _data ??= new();
                return _data;
            }
        }
        InventoryInstance _data;

        public virtual void AddItem(ItemInstance inst)
        {
            if (inst == null || Data.Items.Contains(inst))
                return;

            inst.SwitchInventoryEvent(Data);
            inst.OnSwitchInventory += OnItemSwitch;
            Data.Items.Add(inst);
            Data.Items.RemoveAll((it) => it == null);
        }

        public virtual void RemoveItem(ItemInstance inst)
        {
            if (inst == null || !Data.Items.Contains(inst))
                return;

            inst.OnSwitchInventory -= OnItemSwitch;
            Data.Items.Remove(inst);
            Data.Items.RemoveAll((it) => it == null);
        }

        public virtual WorldItemBase DropItem(ItemInstance inst, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            if (inst == null || !Data.Items.Contains(inst) || inst.Type == null || inst.Type.WorldPrefab == null)
                return null;

            RemoveItem(inst);
            return inst.Type.SpawnItem(inst, position, rotation, parent);
        }

        protected virtual void OnItemSwitch(ItemInstance inst, InventoryInstance inv) => RemoveItem(inst);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class InventoryInstance
    {
        [JsonProperty]
        public List<ItemInstance> Items = new();

        public static string InventoryToJson(InventoryInstance inventory) => JsonConvert.SerializeObject(inventory);
        public static InventoryInstance JsonToInventory(string json) => JsonConvert.DeserializeObject<InventoryInstance>(json);
    }
}
