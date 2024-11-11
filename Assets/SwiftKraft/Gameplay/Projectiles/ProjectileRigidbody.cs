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

        public int Bounces = 0;

        public bool ConstantMotion;

        int currentBounce;

        protected override void Start()
        {
            base.Start();
            Rigidbody.velocity = transform.rotation * Speed;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (ConstantMotion)
                Rigidbody.velocity = transform.rotation * Speed;
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            currentBounce++;

            Vector3 normal = collision.GetContact(0).normal;
            Vector3 velocity = Vector3.Reflect(Rigidbody.velocity, normal);
            transform.forward = velocity;
            Rigidbody.velocity = velocity;

            if (currentBounce > Bounces)
                Hit(collision);
        }

        public virtual void Hit(Collision collision) => Destroy(gameObject);
    }
}
