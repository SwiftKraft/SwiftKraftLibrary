using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    public class DamageDataBase : ICloneable<DamageDataBase>
    {
        public readonly IPawn Attacker;

        public float Damage;
        public Vector3 HitPoint;

        public virtual void ApplyDamage(IDamagable dmg) { }

        public DamageDataBase(float damage, Vector3 hitPoint, IPawn attacker)
        {
            Attacker = attacker;
            Damage = damage;
            HitPoint = hitPoint;
        }

        public virtual DamageDataBase Clone() => new(Damage, HitPoint, Attacker);

    }
}
