using SwiftKraft.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

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

            meshSwapper.ResetMesh();
        }

        public Vector3 DebugMeshOffset;

        public override void DrawGizmos(Transform root)
        {
            base.DrawGizmos(root);

            if (Package.mesh == null || Package.materials.Length <= 0)
                return;

            int subMeshes = Package.mesh.subMeshCount;
            for (int i = 0; i < Mathf.Min(subMeshes, Package.materials.Length); i++)
            {
                Material mat = Package.materials[i];
                Gizmos.color = mat == null ? Color.white : mat.color;
                Gizmos.DrawMesh(Package.mesh, i, root.position + root.rotation * DebugMeshOffset, root.rotation, root.lossyScale);
            }
        }
    }
}
