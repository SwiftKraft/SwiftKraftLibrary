using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentPrefabProperty : WeaponAttachmentSlot.AttachmentProperty
    {
        public Vector3 OffsetPosition;
        public Vector3 OffsetRotation;
        public GameObject Prefab;
        public Transform Workspace;

        protected GameObject Instance;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);

            if (Workspace == null)
                Workspace = this.parent.parent.transform;
        }

        public override void Update()
        {
            if (Instance == null && Prefab != null)
            {
                Instance = Object.Instantiate(Prefab, Workspace);
                Instance.SetActive(false);
                Instance.transform.SetLocalPositionAndRotation(OffsetPosition, Quaternion.Euler(OffsetRotation));
            }

            if (Instance != null)
                Instance.SetActive(true);
        }

        public override void Uninstall()
        {
            base.Uninstall();
            if (Instance != null)
                Instance.SetActive(false);
        }

        public override void Destroy()
        {
            base.Destroy();
            if (Instance != null)
                Object.Destroy(Instance);
        }
    }
}
