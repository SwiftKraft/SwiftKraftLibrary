using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttackBurst : WeaponAttackSingle
    {
        public int Count;
        public float Delay;

        private readonly Timer delay = new();

        public override bool Attacking => base.Attacking || !delay.Ended;

        public bool Firing { get; private set; }

        int count;

        public override bool Attack()
        {
            count = 0;
            Firing = true;
            FireBurst();
            return false;
        }

        public override void Tick()
        {
            base.Tick();

            delay.Tick(Time.fixedDeltaTime);

            if (!Firing || !delay.Ended)
                return;

            UpdateCanAttack();

            if (count < Count && Parent.CanAttack)
                FireBurst();
            else
                StopBurst();

            UpdateCanAttack();
        }

        private void StopBurst()
        {
            count = Count;
            Firing = false;
            TriggerCooldown();
        }

        private bool FireBurst()
        {
            if (base.Attack())
            {
                count++;
                delay.Reset(Delay);
                return true;
            }

            return false;
        }
    }
}
