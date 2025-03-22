using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.UI;
using System;
using UnityEngine;

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

        private void OnEnable()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnDisable()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void SelectSlot(WeaponAttachmentSlot slot)
        {
            CurrentSlot = slot;
            OnSelectSlot?.Invoke(CurrentSlot);
            UpdateAttachments(CurrentSlot);
        }

        public event Action<WeaponAttachmentSlot.Attachment> OnSelectAttachment;

        public void SelectAttachment(WeaponAttachmentSlot.Attachment att)
        {
            if (CurrentSlot != null)
            {
                CurrentSlot.AttachmentIndex = Array.IndexOf(CurrentSlot.Attachments, att);
                OnSelectAttachment?.Invoke(att);
            }
        }

        public void UpdateSlots()
        {
            foreach (Transform tr in slotList)
                if (tr != slotList)
                    Destroy(tr.gameObject);

            foreach (WeaponAttachmentSlot slot in Target.Slots)
                Instantiate(slotPrefab, slotList).GetComponent<AttachmentSlotUI>().Init(this, slot);

            CurrentSlot = null;
        }

        public void UpdateAttachments(WeaponAttachmentSlot slot)
        {
            foreach (Transform tr in attachmentList)
                if (tr != attachmentList)
                    Destroy(tr.gameObject);

            foreach (WeaponAttachmentSlot.Attachment att in slot.Attachments)
                Instantiate(attachmentPrefab, attachmentList).GetComponent<AttachmentUI>().Init(this, att, slot);

            SelectAttachment(slot.Attachments[slot.AttachmentIndex]);
        }
    }
}
