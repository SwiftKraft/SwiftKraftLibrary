using SwiftKraft.Gameplay.Inventory.Items;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttachments : WeaponComponent
    {
        public class Data : ItemDataBase
        {
            public int[] Attachments = new int[0];
        }

        public const string AttachmentSaveID = "Attachments";

        public EquippedItem Item { get; private set; }

        public WeaponAttachmentSlot[] Slots { get; private set; }

        protected Data data;

        protected virtual void Awake()
        {
            Item = GetComponent<EquippedItem>();

            if (Item != null)
            {
                Item.OnEquip += OnEquip;
                Item.OnUnequip += OnUnequip;
            }

            Slots = GetComponentsInChildren<WeaponAttachmentSlot>();
        }

        protected virtual void OnDestroy()
        {
            if (Item != null)
            {
                Item.OnEquip -= OnEquip;
                Item.OnUnequip -= OnUnequip;
            }
        }

        protected virtual void OnUnequip() => SaveData();

        protected virtual void OnEquip() => LoadData();

        public virtual void AttachmentsUpdated() => SaveData();

        public virtual void LoadData()
        {
            Item.Instance.TryData(AttachmentSaveID, out data);
            ApplyData(data.Attachments);
        }

        public virtual void ApplyData(int[] attachments)
        {
            for (int i = 0; i < attachments.Length; i++)
                Slots[i].AttachmentIndex = attachments[i];
        }

        public virtual void SaveData()
        {
            if (data == null)
                LoadData();

            data.Attachments = GetData();
        }

        public virtual int[] GetData()
        {
            int[] attachments = new int[Slots.Length];
            for (int i = 0; i < attachments.Length; i++)
                attachments[i] = Slots[i].AttachmentIndex;
            return attachments;
        }
    }
}
