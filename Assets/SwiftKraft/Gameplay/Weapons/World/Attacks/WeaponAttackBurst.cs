using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttackBurst : WeaponAttackSingle
    {
        public int Count;
        public float Delay;

        private readonly Timer delay = new();

        public override bool Attacking => base.Attacking || !delay.Ended || fireTrigger.Triggered;

        public bool Firing { get; private set; }

        readonly Trigger fireTrigger = new();

        int count;

        public override bool Attack()
        {
            count = 0;
            Firing = true;
            fireTrigger.SetTrigger();
            return false;
        }

        public override void Tick()
        {
            base.Tick();

            delay.Tick(Time.fixedDeltaTime);

            if (!Firing)
                return;

            fireTrigger.SetTrigger(false);

            UpdateCanAttack();

            if (count < Count && Parent.CanAttack)
            {
                if (delay.Ended && base.Attack())
                {
                    count++;
                    delay.Reset(Delay);
                }
            }
            else
            {
                Firing = false;
                TriggerCooldown();
            }
        }
    }
}
