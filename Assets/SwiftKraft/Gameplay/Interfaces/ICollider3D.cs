using UnityEngine;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface ICollider3D
    {
        public Collider[] Colliders { get; }

        public void SetActive(bool active)
        {
            foreach (Collider collider in Colliders)
                collider.enabled = active;
        }

        public void SetActive(params bool[] active)
        {
            for (int i = 0; i < Mathf.Min(active.Length, Colliders.Length); i++)
                Colliders[i].enabled = active[i];
        }
    }
}
