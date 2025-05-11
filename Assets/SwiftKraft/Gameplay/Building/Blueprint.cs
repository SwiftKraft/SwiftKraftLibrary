using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class Blueprint : MonoBehaviour
    {
        public Renderer[] Renderers { get; set; }

        public readonly List<Collider> Intersections = new();

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

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!Intersections.Contains(other))
                Intersections.Add(other);
        }

        protected virtual void OnTriggerExit(Collider other) => Intersections.Remove(other);
    }
}