using SwiftKraft.Gameplay.Interfaces;

namespace SwiftKraft.Gameplay.Damagables
{
    public abstract class DamageDataBase
    {
        public readonly IFaction Attacker;
        public readonly IDamagable Target;

        public DamageDataBase(IFaction attacker, IDamagable target)
        {
            Attacker = attacker;
            Target = target;
        }

        public virtual void ApplyDamage() => Target.Damage(this);
    }
}
