using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ItemInstance
    {
        public ItemType Type
        {
            get
            {
                if (_type == null)
                    _type = typeId;

                return _type;
            }
        }
        ItemType _type;

        [JsonProperty]
        private readonly string typeId;

        public event Action OnDestroy;
        public event Action<ItemInstance, InventoryInstance> OnSwitchInventory;

        public bool Disposed { get; private set; }

        [JsonProperty]
        public Dictionary<string, ItemDataBase> Data { get; private set; } = new();

        [JsonConstructor]
        public ItemInstance(string typeId)
        {
            this.typeId = typeId;
            this.AddInstance();
        }

        public ItemInstance(ItemType type) : this(type.ID) => _type = type;

        public void SwitchInventoryEvent(InventoryInstance inv) => OnSwitchInventory?.Invoke(this, inv);

        public void Despawn()
        {
            OnDestroy?.Invoke();
            Disposed = true;
            this.RemoveInstance();
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ItemDataBase
    {
        public readonly ItemInstance Parent;

        public bool Disposed => Parent.Disposed;

        public ItemDataBase(ItemInstance parent) => Parent = parent;
    }
}
