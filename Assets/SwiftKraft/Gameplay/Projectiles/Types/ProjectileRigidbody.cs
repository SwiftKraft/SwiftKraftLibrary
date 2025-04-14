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
        Vector3 prevVel;

        protected void Start()
        {
            Rigidbody.velocity = transform.rotation * Speed;
            Rigidbody.angularVelocity = Vector3.zero;
            if (ConstantMotion)
                Rigidbody.useGravity = false;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (ConstantMotion)
            {
                Rigidbody.velocity = transform.rotation * Speed;
                Rigidbody.angularVelocity = Vector3.zero;
            }

            prevVel = Rigidbody.velocity;
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!Initialized)
                return;

            currentBounce++;

            if (currentBounce > Bounces)
            {
                EndHit(collision);
                return;
            }
            else
                Hit(collision);

            Vector3 normal = collision.GetContact(0).normal.normalized;
            Vector3 direction = Vector3.Reflect(prevVel.normalized, normal);

#if UNITY_EDITOR
            Debug.DrawRay(collision.GetContact(0).point, prevVel.normalized, Color.green, 5f);
            Debug.DrawRay(collision.GetContact(0).point, normal, Color.blue, 5f);
            Debug.DrawRay(collision.GetContact(0).point, direction, Color.red, 5f);
#endif
            Rigidbody.angularVelocity = Vector3.zero;
            transform.forward = direction.normalized;
            Rigidbody.velocity = Quaternion.LookRotation(direction) * Speed;
        }

        public virtual void EndHit(Collision collision)
        {
            Hit(collision);
            Destroy(gameObject);
        }

        public virtual void Hit(Collision collision)
        {
            HitInfo info = new(collision.GetContact(0));
            HitEvent(info);
            if (info.Object.TryGetComponent(out IDamagable dmg) && (dmg is not IFaction f || f.Faction != Faction))
                dmg.Damage(GetDamageData(info));
        }
    }
}
