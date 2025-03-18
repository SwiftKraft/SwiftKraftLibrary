using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class MeshSwapper : MonoBehaviour
    {
        public SkinnedMeshRenderer SkinnedRenderer { get; private set; }

        Mesh originalMesh;
        Material[] originalMaterials;

        protected virtual void Awake()
        {
            SkinnedRenderer = GetComponent<SkinnedMeshRenderer>();
            originalMesh = SkinnedRenderer.sharedMesh;
            originalMaterials = SkinnedRenderer.materials;
        }

        public void SwapMesh(Package pack) => SwapMesh(pack.mesh, pack.materials);

        public void SwapMesh(Mesh mesh, params Material[] materials)
        {
            SkinnedRenderer.sharedMesh = mesh;
            SkinnedRenderer.materials = materials;
        }

        public void ResetMesh() => SwapMesh(originalMesh, originalMaterials);

        [Serializable]
        public struct Package
        {
            public Mesh mesh;
            public Material[] materials;

            public Package(Mesh m, params Material[] mats)
            {
                mesh = m;
                materials = mats;
            }
        }
    }
}
