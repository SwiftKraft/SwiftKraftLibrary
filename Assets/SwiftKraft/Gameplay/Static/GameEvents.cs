using SwiftKraft.Gameplay.Damagables;
using SwiftKraft.Gameplay.Interfaces;
using System;

namespace SwiftKraft.Gameplay
{
    public static class GameEvents
    {
        public static event Action<IDamagable, DamageDataBase> OnDamage;
        public static void DamageEvent(this IDamagable dmg, DamageDataBase damageData) => OnDamage?.Invoke(dmg, damageData);
    }
}
