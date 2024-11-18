using SwiftKraft.Gameplay.Weapons.Interfaces;
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

            Tracer = GetComponentInChildren<LineRenderer>();

            Lifetime.Reset();

            if (Lifetime.Ended)
                Despawn();
            else
            {
                Lifetime.OnTimerEnd += Despawn;
                Projectile.OnHit += OnHit;
            }
        }

        protected virtual void OnDestroy()
        {
            Lifetime.OnTimerEnd -= Despawn;
            Projectile.OnHit -= OnHit;
        }

        protected virtual void Update() => Lifetime.Tick(Time.deltaTime);

        protected virtual void OnHit()
        {
            int addition = Projectile.HitCount < Projectile.Pierce ? 1 : 0;
            Vector3[] positions = new Vector3[Projectile.HitCount + addition + 1];
            positions[0] = VisualOrigin;
            for (int i = 0; i < Projectile.HitCount + addition; i++)
                positions[i + 1] = i < Projectile.HitCount
                    ? Projectile.Hits[i].point
                    : Projectile.transform.position + Projectile.transform.forward * Projectile.Range;

            Tracer.SetPositions(positions);
        }

        public virtual void Despawn()
        {
            Tracer.enabled = false;
            enabled = false;
        }
    }
}
