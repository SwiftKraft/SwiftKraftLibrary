using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponDelay : WeaponBase
    {
        public override bool Attacking => !Prefire.Ended || !Cooldown.Ended;

        public ModifiableStatistic PrefireDelay = new(0f);
        public ModifiableStatistic CooldownDelay = new(0.1f);

        [HideInInspector]
        public Timer Prefire;
        [HideInInspector]
        public Timer Cooldown;

        [HideInInspector]
        public bool CancelPrefire;

        Transform attackOrigin;

        readonly Trigger attackTrigger = new();

        protected override void Awake()
        {
            base.Awake();
            Prefire.MaxValue = PrefireDelay;
            Cooldown.MaxValue = CooldownDelay;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!CanAttack)
                attackTrigger.SetTrigger(false);

            Prefire.Tick(Time.fixedDeltaTime);
            Cooldown.Tick(Time.fixedDeltaTime);

            if (attackTrigger.GetTrigger() && Prefire.Ended)
                PerformAttack(attackOrigin);
        }

        public override bool Attack(Transform origin)
        {
            if (!Cooldown.Ended || !Prefire.Ended)
                return false;

            attackTrigger.SetTrigger();
            attackOrigin = origin;

            if (!CancelPrefire)
                Prefire.Reset(PrefireDelay);

            if (Prefire.Ended || CancelPrefire)
                return PerformAttack(origin);

            return false;
        }

        public virtual bool PerformAttack(Transform origin)
        {
            bool status = base.Attack(origin);
            attackTrigger.SetTrigger(false);

            if (status)
                Cooldown.Reset(CooldownDelay);

            return status;
        }
    }
}
