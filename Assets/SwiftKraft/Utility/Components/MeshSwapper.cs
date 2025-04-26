using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class MeshSwapper : RequiredDependencyComponent<SkinnedMeshRenderer>
    {
        Mesh originalMesh;
        Material[] originalMaterials;

        protected virtual void Awake()
        {
            originalMesh = Component.sharedMesh;
            originalMaterials = Component.materials;
        }

        public void SwapMesh(Package pack) => SwapMesh(pack.mesh, pack.materials);

        public void SwapMesh(Mesh mesh, params Material[] materials)
        {
            Component.sharedMesh = mesh;
            Component.materials = materials;
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
