using Newtonsoft.Json;
using SwiftKraft.Saving.Data;
using System;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ItemInstance : SaveInstanceBase<ItemDataBase>
    {
        [JsonProperty("guid")]
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

        [JsonProperty("typeId")]
        private readonly string typeId;

        public event Action OnDestroy;
        public event Action OnRefresh;
        public event Action<ItemInstance, InventoryInstance> OnSwitchInventory;

        [JsonConstructor]
        public ItemInstance(Guid guid, string typeId)
        {
            Guid = guid;
            this.typeId = typeId;
            if (!this.AddInstance())
                Disposed = true;
        }

        public ItemInstance(ItemType type) : this(Guid.NewGuid(), type.ID) => _type = type;

        public override void InitializeData<T>(T t)
        {
            base.InitializeData(t);
            t.Init(this);
        }

        public void SwitchInventoryEvent(InventoryInstance inv) => OnSwitchInventory?.Invoke(this, inv);

        public void Refresh()
        {
            OnRefresh?.Invoke();
            if (TryGetData(WorldItemBase.TransformDataID, out WorldItemBase.Data data))
                Type.SpawnItem(this, data.Transform);
        }

        public void Despawn()
        {
            OnDestroy?.Invoke();
            Disposed = true;
            this.RemoveInstance();
        }

        public static implicit operator Guid(ItemInstance inst) => inst.Guid;
        public static implicit operator ItemInstance(Guid guid) => ItemManager.TryGetInstance(guid, out ItemInstance inst) ? inst : null;

        public static string ItemToJson(ItemInstance inst) => JsonConvert.SerializeObject(inst);

        public static ItemInstance JsonToItem(string json) => JsonConvert.DeserializeObject<ItemInstance>(json);
    }

    public abstract class ItemDataBase : SaveDataBase
    {
        public ItemInstance Parent { get; private set; }

        public bool Disposed => Parent.Disposed;

        public void Init(ItemInstance parent) => Parent = parent;
    }
}
