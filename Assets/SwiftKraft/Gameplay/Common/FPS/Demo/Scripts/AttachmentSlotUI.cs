using SwiftKraft.Gameplay.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [RequireComponent(typeof(Button))]
    public class AttachmentSlotUI : MonoBehaviour
    {
        [HideInInspector]
        public WeaponAttachmentSlot Slot;
        WeaponAttachmentMenu parent;

        Button button;
        TMP_Text text;

        private void Awake()
        {
            button = GetComponent<Button>();
            text = GetComponentInChildren<TMP_Text>();
        }

        private void OnDestroy() => parent.OnSelectSlot -= Parent_OnSelectSlot;

        public void Init(WeaponAttachmentMenu menu, WeaponAttachmentSlot slot)
        {
            Slot = slot;
            text.SetText(slot.Scriptable.SlotName);
            parent = menu;
            parent.OnSelectSlot += Parent_OnSelectSlot;
            button.onClick.AddListener(() => parent.SelectSlot(Slot));
        }

        private void Parent_OnSelectSlot(WeaponAttachmentSlot obj) => button.interactable = obj != Slot;
    }
}
