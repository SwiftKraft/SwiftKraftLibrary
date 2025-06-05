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

        public bool Initialized { get; private set; } = false;

        public override void Init()
        {
            if (Projectile == null)
                return;

            VisualOrigin = transform.position;

            Tracer = GetComponentInChildren<LineRenderer>();

            Projectile.OnCast += Show;
        }

        protected virtual void OnDestroy()
        {
            Lifetime.OnTimerEnd -= Despawn;
            Projectile.OnCast -= Show;
        }

        protected virtual void Update()
        {
            if (Initialized)
                Lifetime.Tick(Time.deltaTime);
        }

        public virtual void Show()
        {
            HitPoint = Projectile.Hits.Length > 0 ? Projectile.Hits[Mathf.Min(Projectile.Pierce, Projectile.Hits.Length - 1)].point : transform.position + transform.forward * Projectile.Range;
            ShowLine(VisualOrigin, HitPoint);
            Lifetime.Reset();
            Initialized = true;

            if (Lifetime.Ended)
                Despawn();
            else
                Lifetime.OnTimerEnd += Despawn;
        }

        public virtual void Despawn()
        {
            Tracer.enabled = false;
            enabled = false;
        }

        public virtual void ShowLine(Vector3 position, Vector3 targetPosition)
        {
            float normalizedSpacing = 1f / (Tracer.positionCount - 1);

            Vector3[] positions = new Vector3[Tracer.positionCount];
            for (int i = 0; i < positions.Length - 1; i++)
                positions[i] = Vector3.Lerp(position, targetPosition, normalizedSpacing * i);
            positions[^1] = targetPosition;
            
            Tracer.SetPositions(positions);
        }
    }
}
