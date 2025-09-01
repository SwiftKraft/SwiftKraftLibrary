using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItemModel : MonoBehaviour
    {
        public EquippedItemModelTransferrer Transferrer { get; private set; }

        public string WorkspaceName;

        protected virtual void Start()
        {
            Transferrer = GetComponentInParent<EquippedItemModelTransferrer>();
            Transferrer.Transfer(this);
        }
    }
}
