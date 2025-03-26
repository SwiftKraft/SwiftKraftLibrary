using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttachmentSlot : MeshSwapper
    {
#if UNITY_EDITOR
        public bool DebugMode = false;
#endif
        public string SlotName;

        public WeaponAdsOffset AdsOffset { get; private set; }
        public WeaponAttachments Parent { get; private set; }

        public int AttachmentIndex
        {
            get => _attachmentIndex;
            set
            {
                if (value % Attachments.Length == _attachmentIndex)
                    return;

                _attachmentIndex = value % Attachments.Length;
                UpdateAttachment();
            }
        }
        int _attachmentIndex;

        public Attachment[] Attachments;

        WeaponAdsOffset.Override offset;

        protected override void Awake()
        {
            base.Awake();
            Parent = GetComponentInParent<WeaponAttachments>();
            AdsOffset = GetComponentInParent<WeaponAdsOffset>();
            offset = AdsOffset.AddOverride();

            if (Attachments.Length == 0)
            {
                enabled = false;
                return;
            }

            foreach (Attachment attachment in Attachments)
            {
                attachment.parent = this;
                attachment.Awake();
            }

            UpdateAttachment();
        }

        private void OnDestroy() => offset.Dispose();

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

            if (offset != null)
            {
                offset.TargetPosition = att.targetOffset;
                offset.TargetRotation = Quaternion.Euler(att.targetEulerOffset);
            }
        }

        [Serializable]
        public class Attachment
        {
            [HideInInspector]
            public WeaponAttachmentSlot parent;

            public string name;
            public Package package;

            public readonly List<AttachmentPropertyBase> properties = new();

            public void Awake()
            {
                foreach (AttachmentPropertyBase prop in properties)
                {
                    prop.parent = parent;
                    prop.Awake();
                }
            }

            public void Update()
            {
                foreach (AttachmentPropertyBase prop in properties)
                    prop.Update();
            }

            public Vector3 targetOffset;
            public Vector3 targetEulerOffset;
        }

        [Serializable]
        public abstract class AttachmentPropertyBase
        {
            [HideInInspector]
            public WeaponAttachmentSlot parent;

            public abstract void Awake();
            public abstract void Update();
        }
    }
}