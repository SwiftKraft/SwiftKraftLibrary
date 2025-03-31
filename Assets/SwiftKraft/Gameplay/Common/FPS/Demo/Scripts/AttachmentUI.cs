using SwiftKraft.Gameplay.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [RequireComponent(typeof(Button))]
    public class AttachmentUI : MonoBehaviour
    {
        [HideInInspector]
        public WeaponAttachmentSlotScriptable.Attachment Attachment;
        [HideInInspector]
        public WeaponAttachmentSlot ParentSlot;
        WeaponAttachmentMenu parent;

        Button button;
        TMP_Text text;

        private void Awake()
        {
            button = GetComponent<Button>();
            text = GetComponentInChildren<TMP_Text>();
        }

        private void OnDestroy() => parent.OnSelectAttachment -= Parent_OnSelectAttachment;

        public void Init(WeaponAttachmentMenu menu, WeaponAttachmentSlotScriptable.Attachment att, WeaponAttachmentSlot slot)
        {
            Attachment = att;
            ParentSlot = slot;
            text.SetText(att.name);
            parent = menu;
            parent.OnSelectAttachment += Parent_OnSelectAttachment;
            button.onClick.AddListener(() => parent.SelectAttachment(Attachment));
        }

        private void Parent_OnSelectAttachment(WeaponAttachmentSlotScriptable.Attachment obj) => button.interactable = obj != Attachment;
    }
}
