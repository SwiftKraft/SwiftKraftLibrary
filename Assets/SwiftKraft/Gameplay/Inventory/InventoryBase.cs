using Newtonsoft.Json;
using SwiftKraft.Gameplay.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory
{
    public abstract class InventoryBase : MonoBehaviour
    {
        public bool DropOnDestroy = true;

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
            RemoveInvalid();
        }

        public virtual void RemoveItem(ItemInstance inst)
        {
            if (inst == null || !Data.Items.Contains(inst))
                return;

            inst.OnSwitchInventory -= OnItemSwitch;
            Data.Items.Remove(inst);
            RemoveInvalid();
        }

        public virtual void RemoveInvalid() => Data.Items.RemoveAll((it) => it == 0 || !ItemManager.TryGetInstance(it, out ItemInstance inst) || inst.Disposed);

        public virtual WorldItemBase DropItem(ItemInstance inst, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            if (inst == null || inst.Disposed || !Data.Items.Contains(inst) || inst.Type == null || inst.Type.WorldPrefab == null)
                return null;

            RemoveItem(inst);
            return inst.Type.SpawnItem(inst, position, rotation, parent);
        }

        public virtual void DropInventory(Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            uint[] items = Data.Items.ToArray();
            foreach (ItemInstance it in items)
                DropItem(it, position, rotation, parent);
        }

        protected virtual void OnDestroy()
        {
            if (DropOnDestroy)
                DropInventory(transform.position, transform.rotation);
        }

        protected virtual void OnItemSwitch(ItemInstance inst, InventoryInstance inv) => RemoveItem(inst);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class InventoryInstance
    {
        [JsonProperty]
        public List<uint> Items = new();

        public static string InventoryToJson(InventoryInstance inventory) => JsonConvert.SerializeObject(inventory);
        public static InventoryInstance JsonToInventory(string json) => JsonConvert.DeserializeObject<InventoryInstance>(json);
    }
}
