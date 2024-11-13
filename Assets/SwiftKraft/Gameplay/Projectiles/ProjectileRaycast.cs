using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileRaycast : ProjectileHitscan
    {
        public override void Cast(ref RaycastHit[] hits) => HitCount = Physics.RaycastNonAlloc(transform.position, transform.forward, hits, Range, Layers, TriggerInteraction);

        public override void Hit(RaycastHit[] hits) { }
    }
}
