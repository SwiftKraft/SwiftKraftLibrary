using SwiftKraft.Gameplay.Damagables;
using SwiftKraft.Gameplay.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace SwiftKraft.Gameplay.Map
{
    public class KillZone : MonoBehaviour
    {
        public class KillZoneData : DamageDataBase
        {
            public KillZoneData() : this(Mathf.Infinity, Vector3.zero, null) { }

            public KillZoneData(float damage, Vector3 hitPoint, IPawn attacker) : base(damage, hitPoint, attacker) { }

            public override void ApplyDamage(IDamagable dmg)
            {
                base.ApplyDamage(dmg);

                if (dmg is IHealth hp)
                    hp.CurrentHealth = 0f;
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out IDamagable dmg))
                dmg.Damage(new KillZoneData());
        }
    }
}
