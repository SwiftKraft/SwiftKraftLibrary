using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Attacks/Timer", fileName = "New Attack", order = 1)]
    public class WeaponAttackTimer : WeaponAttackScriptableBase
    {
        public Timer Prefire;
        public Timer Cooldown;

        [HideInInspector]
        public bool CancelPrefire;

        bool triggered;
        Transform attackOrigin;

        public override void Attack(Transform origin)
        {
            if (!Cooldown.Ended || !Prefire.Ended)
                return;

            attackOrigin = origin;
            triggered = true;

            if (!CancelPrefire)
                Prefire.Reset();

            if (Prefire.Ended)
                PerformAttack(origin);
        }

        public virtual void PerformAttack(Transform origin)
        {
            base.Attack(origin);
            triggered = false;
            Cooldown.Reset();
        }

        public override void Tick()
        {
            base.Tick();
            Prefire.Tick(Time.fixedDeltaTime);
            Cooldown.Tick(Time.fixedDeltaTime);

            if (triggered && Prefire.Ended)
                PerformAttack(attackOrigin);
        }
    }
}
