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

        [HideInInspector]
        public Vector3 HitPoint;

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

            Projectile.OnCast += Show;
        }

        protected virtual void OnDestroy()
        {
            Lifetime.OnTimerEnd -= Despawn;
            Projectile.OnCast -= Show;
        }

        protected virtual void Update() => Lifetime.Tick(Time.deltaTime);

        public virtual void Show()
        {
            HitPoint = Projectile.Hits.Length > 0 ? Projectile.Hits[Mathf.Min(Projectile.Pierce, Projectile.Hits.Length - 1)].point : transform.position + transform.forward * Projectile.Range;
            Vector3[] positions = { VisualOrigin, HitPoint };
            Tracer.SetPositions(positions);
        }

        public virtual void Despawn()
        {
            Tracer.enabled = false;
            enabled = false;
        }
    }
}
