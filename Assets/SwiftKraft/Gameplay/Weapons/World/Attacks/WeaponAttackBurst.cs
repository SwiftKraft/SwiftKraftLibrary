using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttackBurst : WeaponAttackSingle
    {
        public int Count;
        public float Delay;

        private readonly Timer delay = new();

        public bool Firing { get; private set; }

        int count;

        public override void Attack()
        {
            count = 0;
            Firing = true;
        }

        public override void Tick()
        {
            base.Tick();

            delay.Tick(Time.fixedDeltaTime);

            if (Firing && count < Count)
            {
                if (delay.Ended)
                {
                    base.Attack();
                    count++;
                    delay.Reset(Delay);
                }
            }
            else
                Firing = false;
        }
    }
}
