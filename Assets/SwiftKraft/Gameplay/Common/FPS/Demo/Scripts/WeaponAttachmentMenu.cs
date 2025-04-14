using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.UI.Menus;
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
                UpdateAttachments(null);
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
        public event Action<WeaponAttachmentSlotScriptable.Attachment> OnSelectAttachment;

        private void Awake() => MenuOpen.UpdateOpen += MenuOpen_UpdateOpen;

        private void OnDestroy() => MenuOpen.UpdateOpen -= MenuOpen_UpdateOpen;

        private void MenuOpen_UpdateOpen(bool obj)
        {
            if (obj)
                Active = false;
        }

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

        public void SelectAttachment(WeaponAttachmentSlotScriptable.Attachment att)
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

            if (Target == null)
                return;

            foreach (WeaponAttachmentSlot slot in Target.Slots)
                Instantiate(slotPrefab, slotList).GetComponent<AttachmentSlotUI>().Init(this, slot);

            CurrentSlot = null;
        }

        public void UpdateAttachments(WeaponAttachmentSlot slot)
        {
            foreach (Transform tr in attachmentList)
                if (tr != attachmentList)
                    Destroy(tr.gameObject);

            if (slot == null)
                return;

            foreach (WeaponAttachmentSlotScriptable.Attachment att in slot.Attachments)
                Instantiate(attachmentPrefab, attachmentList).GetComponent<AttachmentUI>().Init(this, att, slot);

            SelectAttachment(slot.Attachments[slot.AttachmentIndex]);
        }
    }
}
