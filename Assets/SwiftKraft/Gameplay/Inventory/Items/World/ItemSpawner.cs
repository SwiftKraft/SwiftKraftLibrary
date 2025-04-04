using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class ItemSpawner : MonoBehaviour
    {
        public ItemType Item;

        public Transform TargetTransform;
        public bool Parented;

        protected virtual void Awake()
        {
            if (TargetTransform == null)
                TargetTransform = transform;
        }

        public virtual void Spawn() => Item.SpawnItem(null, TargetTransform.position, TargetTransform.rotation, Parented ? TargetTransform : null);
    }
}
