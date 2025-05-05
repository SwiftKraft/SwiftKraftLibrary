using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class Blueprint : MonoBehaviour
    {
        public Renderer[] Renderers { get; set; }

        public void ChangeMaterial(Material material)
        {
            foreach (Renderer renderer in Renderers)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    mats[i] = material;
                renderer.sharedMaterials = mats;
            }
        }
    }
}