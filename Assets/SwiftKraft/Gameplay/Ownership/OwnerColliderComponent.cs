using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Ownership
{
    public class OwnerColliderComponent : OwnerComponentBase, ICollider
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
    }
}
