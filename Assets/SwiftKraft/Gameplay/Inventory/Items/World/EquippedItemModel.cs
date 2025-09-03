using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItemModel : MonoBehaviour
    {
        public EquippedItemModelTransferrer Transferrer { get; private set; }
        public EquippedItemBase Item { get; private set; }

        public string WorkspaceName;

        protected virtual void Awake() => Item = GetComponentInParent<EquippedItemBase>();

        protected virtual void Start()
        {
            Transferrer = GetComponentInParent<EquippedItemModelTransferrer>();
            Transferrer.Transfer(this);

            Item.OnObjectEnable += OnEquippableEnable;
            Item.OnObjectDisable += OnEquippableDisable;
        }

        protected virtual void OnDestroy()
        {
            Item.OnObjectEnable -= OnEquippableEnable;
            Item.OnObjectDisable -= OnEquippableDisable;
        }

        protected virtual void OnEquippableDisable() => gameObject.SetActive(false);

        protected virtual void OnEquippableEnable() => gameObject.SetActive(true);
    }
}
