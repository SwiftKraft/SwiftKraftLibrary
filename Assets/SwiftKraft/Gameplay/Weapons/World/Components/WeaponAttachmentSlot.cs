using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttachmentSlot : MeshSwapper
    {
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

        protected override void Awake()
        {
            base.Awake();
            Parent = GetComponentInParent<WeaponAttachments>();
            AdsOffset = GetComponentInParent<WeaponAdsOffset>();

            if (Attachments.Length == 0)
            {
                enabled = false;
                return;
            }

            UpdateAttachment();
        }

        public void UpdateAttachment()
        {
            Attachment att = Attachments[AttachmentIndex];
            SwapMesh(att.package);

            if (AdsOffset != null)
            {
                AdsOffset.TargetPosition = att.targetOffset;
                AdsOffset.TargetRotation = Quaternion.Euler(att.targetEulerOffset);
            }
        }

        [Serializable]
        public struct Attachment
        {
            public string name;
            public Package package;

            public Vector3 targetOffset;
            public Vector3 targetEulerOffset;
        }
    }
}