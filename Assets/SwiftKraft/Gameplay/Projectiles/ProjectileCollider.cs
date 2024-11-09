using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileCollider : ProjectileBase, ICollider
    {
        public Collider[] Colliders
        {
            get
            {
                _colliders ??= GetComponentsInChildren<Collider>();
                return _colliders;
            }
        }
        Collider[] _colliders;

        protected virtual void Start()
        {
            if (Owner is ICollider col)
                foreach (Collider c1 in Colliders)
                    foreach (Collider c2 in col.Colliders)
                        Physics.IgnoreCollision(c1, c2, true);
        }
    }
}
