using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileRigidbody : ProjectileCollider
    {
        public Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody>();

                return _rigidbody;
            }
        }
        Rigidbody _rigidbody;

        public Vector3 Speed;

        public bool ConstantMotion;

        protected override void Start()
        {
            base.Start();
            Rigidbody.velocity = transform.rotation * Speed;
        }

        protected virtual void FixedUpdate()
        {
            if (ConstantMotion)
                Rigidbody.velocity = transform.rotation * Speed;
        }
    }
}
