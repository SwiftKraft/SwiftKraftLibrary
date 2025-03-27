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

        public WeaponAdsOffset AdsOffset { get; private set; }
        public WeaponShootPoint ShootPoint { get; private set; }
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

        WeaponAdsOffset.Override offsetAds;
        WeaponShootPoint.Override offsetShootPoint;

        protected override void Awake()
        {
            base.Awake();
            Parent = GetComponentInParent<WeaponAttachments>();
            AdsOffset = GetComponentInParent<WeaponAdsOffset>();
            ShootPoint = GetComponentInParent<WeaponShootPoint>();
            if (AdsOffset != null)
                offsetAds = AdsOffset.AddOverride();
            if (ShootPoint != null)
                offsetShootPoint = ShootPoint.AddOverride();

            if (Attachments.Length == 0)
            {
                enabled = false;
                return;
            }

            foreach (Attachment att in Attachments)
                att.Init(this);

            UpdateAttachment();
        }

        private void OnDestroy() => offsetAds.Dispose();

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

            if (offsetShootPoint != null)
                offsetShootPoint.OverridePosition = att.shootPointOffset;

            if (offsetAds != null)
            {
                offsetAds.TargetPosition = att.targetOffset;
                offsetAds.TargetRotation = Quaternion.Euler(att.targetEulerOffset);
            }
        }

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

            [Header("Shoot Point Offset")]
            public Vector3 shootPointOffset;

            [Header("Aim Offset")]
            public Vector3 targetOffset;
            public Vector3 targetEulerOffset;
        }

        [Serializable]
        public abstract class AttachmentProperty
        {
            [HideInInspector]
            public Attachment parent;

            public virtual void Init(Attachment parent) => this.parent = parent;

            public abstract void Update();
        }
    }
}