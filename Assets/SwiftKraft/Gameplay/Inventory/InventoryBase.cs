using Newtonsoft.Json;
using SwiftKraft.Gameplay.Inventory.Items;
using System.Collections;
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
            if (Data.Items.Contains(inst))
                return;

            inst.SwitchInventoryEvent(Data);
            inst.OnSwitchInventory += OnItemSwitch;
            Data.Items.Add(inst);
            Data.Items.RemoveAll((it) => it == null);
        }

        public virtual void RemoveItem(ItemInstance inst)
        {
            inst.OnSwitchInventory -= OnItemSwitch;
            Data.Items.Remove(inst);
            Data.Items.RemoveAll((it) => it == null);
        }

        protected virtual void OnItemSwitch(ItemInstance inst, InventoryInstance inv) => RemoveItem(inst);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class InventoryInstance
    {
        [JsonProperty]
        public List<ItemInstance> Items = new();
    }
}
