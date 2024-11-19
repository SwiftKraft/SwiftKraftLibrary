using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileRaycast : ProjectileHitscan
    {
        public override RaycastHit[] Cast() => Physics.RaycastAll(transform.position, transform.forward, Range, Layers, TriggerInteraction);

        public override void Hit(RaycastHit[] hits) { }
    }
}
