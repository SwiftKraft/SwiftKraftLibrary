using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileRaycast : ProjectileHitscan
    {
        public override int Cast(ref RaycastHit[] hits) => Physics.RaycastNonAlloc(transform.position, transform.forward, hits, Range, Layers, TriggerInteraction);

        public override void Hit(RaycastHit[] hits) { }
    }
}
