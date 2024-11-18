using System;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public abstract class ProjectileHitscan : ProjectileBase
    {
        public RaycastHit[] Hits;

        public float Range = 100f;
        public LayerMask Layers;
        public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;

        public int Pierce = 1;
        public int HitCount { get; protected set; }

        public event Action OnHit;

        protected virtual void Start()
        {
            Hits = new RaycastHit[Pierce];
            HitCount = Cast(ref Hits);
            Hits.OrderBy((h) => h.distance);
            OnHit?.Invoke();
            Hit(Hits);
        }

        public abstract int Cast(ref RaycastHit[] hits);

        public abstract void Hit(RaycastHit[] hits);
    }
}
