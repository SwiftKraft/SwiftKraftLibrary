using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAttackCooldown : WeaponAttackBase
    {
        public ModifiableStatistic PrefireDelay = new(0f);
        public ModifiableStatistic CooldownDelay = new(0.1f);

        public override bool Attacking => !prefire.Ended || !cooldown.Ended;

        protected BooleanLock.Lock CanAttack;
        protected Transform CurrentAttackTransform { get; private set; }

        private readonly Timer prefire = new();
        private readonly Timer cooldown = new();

        private readonly Trigger queueAttack = new();

        public override void Begin()
        {
            base.Begin();

            prefire.MaxValue = PrefireDelay;
            cooldown.MaxValue = CooldownDelay;

            CanAttack = Parent.CanAttack.AddLock();
            CanAttack.Active = false;
        }

        public override void Tick()
        {
            base.Tick();

            prefire.Tick(Time.fixedDeltaTime);
            cooldown.Tick(Time.fixedDeltaTime);

            if (prefire.Ended && queueAttack.GetTrigger() && Attack())
                TriggerCooldown();

            UpdateCanAttack();
        }

        public override void End()
        {
            base.End();

            Parent.CanAttack.RemoveLock(CanAttack);
        }

        public override void Attack(Transform trans)
        {
            CurrentAttackTransform = trans;

            if (PrefireDelay > 0f)
            {
                prefire.Reset();
                queueAttack.SetTrigger();
                return;
            }

            if (Attack())
                TriggerCooldown();
        }

        public abstract bool Attack();

        protected void TriggerCooldown() => cooldown.Reset();

        protected void UpdateCanAttack() => CanAttack.Active = Attacking;
    }
}
