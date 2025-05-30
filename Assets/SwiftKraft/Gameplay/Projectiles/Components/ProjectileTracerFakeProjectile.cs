using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileTracerFakeProjectile : ProjectileTracer
    {
        public float LengthPercentage = 0.1f;

        protected override void Update()
        {
            if (Tracer != null)
                Tracer.SetPositions(new Vector3[] { Vector3.Lerp(VisualOrigin, HitPoint, 1f - Lifetime.GetPercentage()), Vector3.Lerp(VisualOrigin, HitPoint, 1f - Lifetime.GetPercentage() + LengthPercentage) });
            base.Update();
        }
    }
}
