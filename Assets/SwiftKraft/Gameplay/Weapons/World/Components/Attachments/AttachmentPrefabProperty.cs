using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentPrefabProperty : WeaponAttachmentSlotScriptable.AttachmentProperty
    {
        public Vector3 OffsetPosition;
        public Vector3 OffsetRotation;
        public GameObject Prefab;
        public string WorkspaceName;

        protected GameObject Instance;
        protected Transform Workspace;

        public override void Init(WeaponAttachmentSlotScriptable.Attachment parent)
        {
            base.Init(parent);

            Workspace = string.IsNullOrEmpty(WorkspaceName) ? this.parent.parent.transform : this.parent.parent.Parent.transform.Find(WorkspaceName);
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

        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentPrefabProperty()
            {
                OffsetPosition = OffsetPosition,
                OffsetRotation = OffsetRotation,
                Prefab = Prefab,
                Workspace = Workspace,
                WorkspaceName = WorkspaceName,
            };
    }
}
