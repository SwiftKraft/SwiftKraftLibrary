using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class WeaponAttachmentMenu : MenuBase
    {
        public WeaponAttachments Target
        {
            get => _target;
            set
            {
                _target = value;
                UpdateSlots();
            }
        }
        WeaponAttachments _target;

        [HideInInspector]
        public WeaponAttachmentSlot CurrentSlot;

        [SerializeField]
        Transform slotList;
        [SerializeField]
        Transform attachmentList;

        [SerializeField]
        GameObject slotPrefab;
        [SerializeField]
        GameObject attachmentPrefab;

        public event Action<WeaponAttachmentSlot> OnSelectSlot;

        public void SelectSlot(WeaponAttachmentSlot slot) => OnSelectSlot?.Invoke(slot);

        public event Action<WeaponAttachmentSlot.Attachment> OnSelectAttachment;

        public void SelectAttachment(WeaponAttachmentSlot.Attachment index) => OnSelectAttachment?.Invoke(index);

        public void UpdateSlots()
        {
            foreach (Transform tr in slotList)
                if (tr != slotList)
                    Destroy(tr.gameObject);

            foreach (WeaponAttachmentSlot slot in Target.Slots)
                Instantiate(slotPrefab, slotList).GetComponent<AttachmentSlotUI>().Init(this, slot);
        }

        public void UpdateAttachments(WeaponAttachmentSlot slot)
        {
            foreach (Transform tr in attachmentList)
                if (tr != attachmentList)
                    Destroy(tr.gameObject);

            foreach (WeaponAttachmentSlot.Attachment att in slot.Attachments)
                Instantiate(attachmentPrefab, attachmentList);
        }
    }
}
