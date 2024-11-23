using Newtonsoft.Json;
using System;
using Object = UnityEngine.Object;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ItemInstance : Object
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

        [JsonConstructor]
        public ItemInstance(string typeId)
        {
            this.typeId = typeId;
            this.AddInstance();
        }

        public ItemInstance(ItemType type) : this(type.ID) => _type = type;

        public void Despawn()
        {
            OnDestroy?.Invoke();
            this.RemoveInstance();
            Destroy(this);
        }
    }
}
