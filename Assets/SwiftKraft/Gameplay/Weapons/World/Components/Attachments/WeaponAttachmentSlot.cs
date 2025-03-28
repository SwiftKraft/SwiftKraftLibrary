using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttachmentSlot : MeshSwapper
    {
#if UNITY_EDITOR
        public bool DebugMode = false;
#endif
        public string SlotName;

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

        public Attachment[] Attachments;

        protected override void Awake()
        {
            base.Awake();
            Parent = GetComponentInParent<WeaponAttachments>();

            if (Attachments.Length == 0)
            {
                enabled = false;
                return;
            }

            foreach (Attachment att in Attachments)
                att.Init(this);

            UpdateAttachment();
        }

        private void OnDestroy()
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

        public void UpdateAttachment()
        {
            Attachment att = Attachments[AttachmentIndex];
            SwapMesh(att.package);

            att.Update();
        }

        public void Uninstall(int index) => Attachments[AttachmentIndex].Uninstall();

        [Serializable]
        public class Attachment
        {
            [HideInInspector]
            public WeaponAttachmentSlot parent;

            public string name;
            public Package package;

            [SerializeReference, Subclass]
            public AttachmentProperty[] properties;

            public void Init(WeaponAttachmentSlot parent)
            {
                this.parent = parent;
                foreach (AttachmentProperty prop in properties)
                    prop.Init(this);
            }

            public void Update()
            {
                foreach (AttachmentProperty prop in properties)
                    prop.Update();
            }

            public void Uninstall()
            {
                foreach (AttachmentProperty prop in properties)
                    prop.Uninstall();
            }

            public void Destroy()
            {
                foreach (AttachmentProperty prop in properties)
                    prop.Destroy();
            }
        }

        [Serializable]
        public abstract class AttachmentProperty
        {
            [HideInInspector]
            public Attachment parent;

            public virtual void Init(Attachment parent) => this.parent = parent;

            public abstract void Update();

            public virtual void Uninstall() { }

            public virtual void Destroy() { }
        }
    }
}