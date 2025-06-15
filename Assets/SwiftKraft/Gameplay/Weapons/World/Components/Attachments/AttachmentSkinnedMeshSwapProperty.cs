using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentSkinnedMeshSwapProperty : WeaponAttachmentSlotScriptable.AttachmentProperty
    {
        public MeshSwapper.Package Package;

        MeshSwapper meshSwapper;

        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() => new AttachmentSkinnedMeshSwapProperty()
        {
            Package = Package,
        };

        public override void Init(WeaponAttachmentSlotScriptable.Attachment parent)
        {
            base.Init(parent);
            if (!parent.parent.TryGetComponent(out meshSwapper))
                meshSwapper = parent.parent.gameObject.AddComponent<MeshSwapper>();
        }

        public override void Update()
        {
            if (meshSwapper == null)
                return;

            meshSwapper.SwapMesh(Package);
        }

        public override void Uninstall()
        {
            base.Uninstall();

            meshSwapper.SwapMesh(null);
        }
    }
}
