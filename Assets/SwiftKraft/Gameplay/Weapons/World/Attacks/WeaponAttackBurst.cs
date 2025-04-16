using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttackBurst : WeaponAttackSingle
    {
        public int Count;

        public Timer Delay;

        public bool Firing { get; private set; }

        Transform attackOrigin;

        int count;

        public override void Attack(Transform origin)
        {
            if (!Parent.CanAttack)
                return;

            attackOrigin = origin;
            count = 0;
            Firing = true;
        }

        public override void Tick()
        {
            base.Tick();

            Delay.Tick(Time.fixedDeltaTime);

            if (Parent.CanAttack && Firing && count < Count)
            {
                if (Delay.Ended)
                {
                    base.Attack(attackOrigin);
                    count++;
                    Delay.Reset();
                }
            }
            else
                Firing = false;
        }
    }
}
