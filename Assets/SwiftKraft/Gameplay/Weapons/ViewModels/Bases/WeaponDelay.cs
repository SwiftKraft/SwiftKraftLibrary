using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponDelay : WeaponBase
    {
        public Timer Prefire;
        public Timer Cooldown;

        Transform attackOrigin;

        bool triggered;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            Prefire.Tick(Time.fixedDeltaTime);
            Cooldown.Tick(Time.fixedDeltaTime);

            if (triggered && Prefire.Ended)
                PerformAttack(attackOrigin);
        }

        public override bool Attack(Transform origin)
        {
            if (!Cooldown.Ended || !Prefire.Ended)
                return false;

            triggered = true;
            attackOrigin = origin;
            Prefire.Reset();

            if (Prefire.Ended)
                return PerformAttack(origin);

            return true;
        }

        public virtual bool PerformAttack(Transform origin)
        {
            bool status = base.Attack(origin);

            if (status)
            {
                triggered = false;
                Cooldown.Reset();
            }

            return status;
        }
    }
}
