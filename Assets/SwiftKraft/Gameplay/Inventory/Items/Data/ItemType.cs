using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Inventory/Item/Basic", fileName = "Basic", order = 0)]
    public class ItemType : ScriptableObject
    {
        public string ID;
        public GameObject WorldPrefab;

        public virtual WorldItemBase SpawnItem(ItemInstance item = null, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (item == null)
                return SpawnItem(new(this), position, rotation, parent);

            if (WorldPrefab == null || !WorldPrefab.TryGetComponent(out WorldItemBase wib))
                return null;

            WorldItemBase inst = Instantiate(wib, position, rotation, parent);
            inst.Init(item);
            return inst;
        }

        public virtual WorldItemBase SpawnItem(ItemInstance item = null, TransformData data = default, Transform parent = null) => SpawnItem(item, data.Position, data.Rotation, parent);

        public static implicit operator ItemType(string id) => ItemManager.GetRegistered(id);
    }
}
