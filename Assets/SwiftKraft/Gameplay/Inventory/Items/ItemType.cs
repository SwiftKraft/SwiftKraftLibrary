using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Inventory/Item/Basic", fileName = "Basic", order = 0)]
    public class ItemType : ScriptableObject
    {
        public string ID;
        public GameObject WorldPrefab;

        public static implicit operator ItemType(string id) => ItemManager.Get(id);
    }
}
