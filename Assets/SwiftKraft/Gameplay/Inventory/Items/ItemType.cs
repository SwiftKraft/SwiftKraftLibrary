using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class ItemType : ScriptableObject
    {
        public string ID;
        public GameObject WorldPrefab;

        public static implicit operator ItemType(string id) => ItemManager.Get(id);
    }
}
