using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileRaycast : ProjectileHitscan
    {
        public override RaycastHit[] Cast() => Physics.RaycastAll(transform.position, transform.forward, Range, Layers, TriggerInteraction);

        public override void Hit(RaycastHit[] hits)
        {
            int cur = 0;
            foreach (RaycastHit hit in hits)
            {
                if (cur > Pierce)
                    return;

                HitInfo info = new(hit);
                HitEvent(info);
                if (info.Object.TryGetComponent(out IDamagable dmg))
                    dmg.Damage(GetDamageData(info));
                cur++;
            }
        }
    }
}
