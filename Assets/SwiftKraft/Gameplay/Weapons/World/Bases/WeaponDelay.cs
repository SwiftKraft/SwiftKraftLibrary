using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponDelay : WeaponBase
    {
        public override bool Attacking => !Prefire.Ended || !Cooldown.Ended;

        public Timer Prefire;
        public Timer Cooldown;

        [HideInInspector]
        public bool CancelPrefire;

        Transform attackOrigin;

        bool triggered;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!CanAttack)
                triggered = false;

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

            if (!CancelPrefire)
                Prefire.Reset();

            if (Prefire.Ended || CancelPrefire)
                return PerformAttack(origin);

            return false;
        }

        public virtual bool PerformAttack(Transform origin)
        {
            bool status = base.Attack(origin);
            triggered = false;

            if (status)
                Cooldown.Reset();

            return status;
        }
    }
}
