using SwiftKraft.Gameplay.Interfaces;

namespace SwiftKraft.Gameplay.Damagables
{
    public class DamageDataBase
    {
        public readonly IPawn Attacker;

        public readonly float Damage;

        public DamageDataBase(float damage, IPawn attacker)
        {
            Attacker = attacker;
            Damage = damage;
        }
    }
}
