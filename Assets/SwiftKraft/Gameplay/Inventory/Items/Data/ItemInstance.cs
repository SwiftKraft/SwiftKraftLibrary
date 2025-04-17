using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ItemInstance
    {
        [JsonProperty]
        public readonly Guid Guid;

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
        public ItemInstance(Guid guid, string typeId)
        {
            Guid = guid;
            this.typeId = typeId;
            if (!this.AddInstance())
                Disposed = true;
        }

        public ItemInstance(ItemType type) : this(Guid.NewGuid(), type.ID) => _type = type;

        public void SwitchInventoryEvent(InventoryInstance inv) => OnSwitchInventory?.Invoke(this, inv);

        public void Despawn()
        {
            OnDestroy?.Invoke();
            Disposed = true;
            this.RemoveInstance();
        }

        public T AddData<T>(string id) where T : ItemDataBase, new()
        {
            if (Data.ContainsKey(id))
                return null;

            T t = new();
            t.Init(this);
            Data.Add(id, t);
            return t;
        }

        public bool TryAddData<T>(string id, out T dat) where T : ItemDataBase, new()
        {
            dat = AddData<T>(id);
            return dat != null;
        }

        public T GetData<T>(string id) where T : ItemDataBase => Data.ContainsKey(id) && Data[id] is T t ? t : null;

        public bool TryGetData<T>(string id, out T dat) where T : ItemDataBase
        {
            dat = GetData<T>(id);
            return dat != null;
        }

        public bool TryData<T>(string id, out T dat) where T : ItemDataBase, new() => TryGetData(id, out dat) || TryAddData(id, out dat);

        public static string ItemToJson(ItemInstance inst) => JsonConvert.SerializeObject(inst);

        public static ItemInstance JsonToItem(string json) => JsonConvert.DeserializeObject<ItemInstance>(json);
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ItemDataBase
    {
        public ItemInstance Parent { get; private set; }

        public bool Disposed => Parent.Disposed;

        public void Init(ItemInstance parent) => Parent = parent;
    }
}
