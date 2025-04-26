using SwiftKraft.Gameplay.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileRaycast : ProjectileHitscan
    {
        public bool HitFriendly;

        public override RaycastHit[] Cast()
        {
            List<RaycastHit> hits = new(Physics.RaycastAll(transform.position, transform.forward, Range, Layers, TriggerInteraction));
            hits.RemoveAll((h) => h.collider.TryGetComponent(out IFaction faction) && faction.Faction == Faction);
            return hits.ToArray();
        }

        public override void Hit(RaycastHit[] hits)
        {
            int cur = 0;
            foreach (RaycastHit hit in hits)
            {
                if (cur > Pierce)
                    return;

                HitInfo info = new(hit);
                HitEvent(info);
                if (info.Object.TryGetComponent(out IDamagable dmg) && (dmg is not IFaction f || f.Faction != Faction))
                    dmg.Damage(GetDamageData(info));
                if (info.Object.TryGetComponent(out Rigidbody rb))
                    rb.AddForceAtPosition(transform.forward * BaseDamage, info.Position, ForceMode.Impulse);
                cur++;
            }
        }
    }
}
