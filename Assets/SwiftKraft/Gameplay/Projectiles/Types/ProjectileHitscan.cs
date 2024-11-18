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

        protected override void Awake()
        {
            base.Awake();
            Hits = new RaycastHit[Pierce];
            Cast(ref Hits);
            Hits.OrderBy((h) => h.distance);
            Hit(Hits);
        }

        public abstract void Cast(ref RaycastHit[] hits);

        public abstract void Hit(RaycastHit[] hits);
    }
}
