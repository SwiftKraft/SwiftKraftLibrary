using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    [RequireComponent(typeof(ProjectileHitscan))]
    public class ProjectileTracer : ProjectileComponent<ProjectileHitscan>, IVisualOrigin
    {
        public LineRenderer Tracer { get; private set; }
        public Vector3 VisualOrigin { get; set; }

        public Timer Lifetime;

        public override void Init()
        {
            if (Projectile == null)
                return;

            VisualOrigin = transform.position;

            Tracer = GetComponentInChildren<LineRenderer>();

            Lifetime.Reset();

            if (Lifetime.Ended)
                Despawn();
            else
                Lifetime.OnTimerEnd += Despawn;
        }

        protected virtual void Start()
        {
            Vector3[] positions = { VisualOrigin, Projectile.Hits.Length > 0 ? Projectile.Hits[^1].point : transform.position + transform.forward * Projectile.Range };
            Tracer.SetPositions(positions);
        }

        protected virtual void OnDestroy() => Lifetime.OnTimerEnd -= Despawn;

        protected virtual void Update() => Lifetime.Tick(Time.deltaTime);

        public virtual void Despawn()
        {
            Tracer.enabled = false;
            enabled = false;
        }
    }
}
