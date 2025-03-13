using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    public class DamageDataBase
    {
        public readonly IPawn Attacker;

        public readonly float Damage;
        public readonly Vector3 HitPoint;

        public DamageDataBase(float damage, Vector3 hitPoint, IPawn attacker)
        {
            Attacker = attacker;
            Damage = damage;
            HitPoint = hitPoint;
        }
    }
}
