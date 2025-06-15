using UnityEngine;
using static SwiftKraft.Gameplay.Weapons.WeaponAttachmentSlotScriptable;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttachmentSlot : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool DebugMode = false;
#endif
        public WeaponAttachmentSlotScriptable Scriptable;

        public Attachment[] Attachments
        {
            get
            {
                if (_attachments == null)
                    RefreshAttachments();

                return _attachments;
            }
        }
        Attachment[] _attachments;

        public WeaponAttachments Parent { get; private set; }

        public int AttachmentIndex
        {
            get => _attachmentIndex;
            set
            {
                if (value % Attachments.Length == _attachmentIndex)
                    return;

                Uninstall(_attachmentIndex);
                _attachmentIndex = value % Attachments.Length;
                UpdateAttachment();
            }
        }
        int _attachmentIndex;

        protected virtual void Awake()
        {
            Parent = GetComponentInParent<WeaponAttachments>();

            if (Attachments.Length == 0)
            {
                enabled = false;
                return;
            }

            RefreshAttachments();

            foreach (Attachment att in Attachments)
                att.Init(this);
        }

        protected virtual void Start() => UpdateAttachment();

        protected virtual void OnDestroy()
        {
            foreach (Attachment att in Attachments)
                att.Destroy();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (DebugMode)
                UpdateAttachment();
        }
#endif

        public void RefreshAttachments()
        {
            _attachments = new Attachment[Scriptable.Attachments.Length];
            for (int i = 0; i < _attachments.Length; i++)
                _attachments[i] = Scriptable.Attachments[i].Clone();
        }

        public void UpdateAttachment()
        {
            Attachment att = Attachments[AttachmentIndex];

            att.Update();
            Parent.AttachmentsUpdated();
        }

        public void Uninstall(int index) => Attachments[index].Uninstall();
    }
}