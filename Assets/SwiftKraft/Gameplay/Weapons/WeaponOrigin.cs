using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponOrigin : MonoBehaviour, IVisualOrigin
    {
        public virtual Vector3 VisualOrigin { get => CurrentOrigin.position; set => CurrentOrigin.localPosition = value; }

        [field: SerializeField]
        public Transform CurrentOrigin { get; protected set; }

        protected virtual void Awake()
        {
            if (CurrentOrigin == null)
                CurrentOrigin = transform;
        }
    }
}
