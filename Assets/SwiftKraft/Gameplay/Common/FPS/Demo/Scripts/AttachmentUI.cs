using SwiftKraft.Gameplay.Weapons;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [RequireComponent(typeof(Button))]
    public class AttachmentUI : MonoBehaviour
    {
        [HideInInspector]
        public WeaponAttachmentSlot.Attachment Attachment;
        WeaponAttachmentMenu parent;

        Button button;
        TMP_Text text;

        private void Awake()
        {
            button = GetComponent<Button>();
            text = GetComponentInChildren<TMP_Text>();
        }

        private void OnDestroy() => parent.OnSelectSlot -= Parent_OnSelectAttachment;

        public void Init(WeaponAttachmentMenu menu, WeaponAttachmentSlot.Attachment att)
        {
            Attachment = att;
            text.SetText(att.name);
            parent = menu;
            parent.OnSelectAttachment += Parent_OnSelectAttachment;
            button.onClick.AddListener(() => parent.SelectAttachment(Array.IndexOf(parent.CurrentSlot.Attachments, Attachment)));
        }

        private void Parent_OnSelectAttachment(WeaponAttachmentSlot.Attachment obj) => button.interactable = obj != Slot;
    }
}
