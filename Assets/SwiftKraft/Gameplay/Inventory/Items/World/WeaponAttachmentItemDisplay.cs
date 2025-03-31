using SwiftKraft.Gameplay.Weapons;
using System.Collections.Generic;
using System.Linq;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class WeaponAttachmentItemDisplay : WorldItemAddonBase
    {
        WeaponAttachments.Data data;

        List<WeaponAttachmentSlotItemDisplay> slots;

        public override void Init(WorldItemBase parent)
        {
            slots = GetComponentsInChildren<WeaponAttachmentSlotItemDisplay>().ToList();
            slots.Sort((a, b) => a.Scriptable.name.CompareTo(b.Scriptable.name));

            base.Init(parent);
            if (!Parent.Item.TryGetData(WeaponAttachments.AttachmentSaveID, out data))
            {
                SetAttachments(new int[slots.Count]);
                return;
            }

            SetAttachments(data.Attachments);
        }

        public void SetAttachments(int[] atts)
        {
            for (int i = 0; i < atts.Length; i++)
                slots[i].SetAttachment(atts[i]);
        }
    }
}
