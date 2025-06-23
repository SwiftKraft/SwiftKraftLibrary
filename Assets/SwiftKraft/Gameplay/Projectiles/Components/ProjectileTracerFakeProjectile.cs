using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileTracerFakeProjectile : ProjectileTracer
    {
        public float LengthPercentage = 0.1f;

        float dist;
        float ratio;
        public override void Show()
        {
            base.Show();
            dist = (HitPoint - VisualOrigin).magnitude;
            ratio = dist / Projectile.Range;
            Lifetime.Reset(Lifetime.MaxValue * ratio);
        }

        protected override void Update()
        {
            if (Tracer != null)
                ShowLine(Vector3.Lerp(VisualOrigin, HitPoint, 1f - Lifetime.GetPercentage()), Vector3.Lerp(VisualOrigin, HitPoint, 1f - Lifetime.GetPercentage() + LengthPercentage * (1f / ratio)));
            base.Update();
        }
    }
}
