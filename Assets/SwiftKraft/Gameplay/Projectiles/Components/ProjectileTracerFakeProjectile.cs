using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileTracerFakeProjectile : ProjectileTracer
    {
        public float LengthPercentage = 0.1f;

        public override void Show()
        {
            base.Show();
            float dist = (HitPoint - VisualOrigin).magnitude;
            Lifetime.Reset(Lifetime.MaxValue * (dist / Projectile.Range));
        }

        protected override void Update()
        {
            if (Tracer != null)
                Tracer.SetPositions(new Vector3[] { Vector3.Lerp(VisualOrigin, HitPoint, 1f - Lifetime.GetPercentage()), Vector3.Lerp(VisualOrigin, HitPoint, 1f - Lifetime.GetPercentage() + LengthPercentage) });
            base.Update();
        }
    }
}
