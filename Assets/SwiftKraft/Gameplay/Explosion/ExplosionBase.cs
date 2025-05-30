using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Gameplay.Damagables;
using SwiftKraft.Gameplay.Interfaces;

namespace SwiftKraft.Gameplay.Explosions
{
    public abstract class ExplosionBase : PetBehaviourBase
    {
        public abstract DamageDataBase GetDamageData();

        public abstract IDamagable[] GetDamagables();

        public virtual void Explode()
        {
            IDamagable[] damagables = GetDamagables();

            for (int i = 0; i < damagables.Length; i++)
                damagables[i].Damage(GetDamageData());
        }
    }
}
