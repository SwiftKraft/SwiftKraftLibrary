using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Gameplay.Damagables;
using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public abstract class ProjectileBase : PetBehaviourBase
    {
        public Timer Lifetime;

        public bool Despawned { get; private set; }

        public float BaseDamage { get; set; }

        public ProjectileComponent[] Addons { get; private set; }

        public event Action<HitInfo> OnHit;
        protected void HitEvent(HitInfo info) => OnHit?.Invoke(info);

        protected virtual void Awake()
        {
            Addons = GetComponents<ProjectileComponent>();
            foreach (ProjectileComponent comp in Addons)
                comp.Init();
            Lifetime.Reset();
        }

        protected virtual void FixedUpdate()
        {
            if (Despawned)
                return;

            Lifetime.Tick(Time.fixedDeltaTime);
            if (Lifetime.Ended)
            {
                Despawned = true;
                Despawn();
            }
        }

        public virtual void Despawn() => Destroy(gameObject);

        public virtual DamageDataBase GetDamageData(HitInfo hitInfo) => new(BaseDamage, hitInfo.Position, Owner);

        public struct HitInfo
        {
            public Vector3 Position;
            public Vector3 Normal;
            public GameObject Object;

            public HitInfo(ContactPoint point) { Position = point.point; Normal = point.normal; Object = point.otherCollider.gameObject; }
            public HitInfo(ContactPoint2D point) { Position = point.point; Normal = point.normal; Object = point.otherCollider.gameObject; }
            public HitInfo(RaycastHit point) { Position = point.point; Normal = point.normal; Object = point.transform.gameObject; }
            public HitInfo(RaycastHit2D point) { Position = point.point; Normal = point.normal; Object = point.transform.gameObject; }
        }
    }
}
