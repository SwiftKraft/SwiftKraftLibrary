using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Attacks/Burst", fileName = "New Attack", order = 1)]
    public class WeaponAttackScriptableBurst : WeaponAttackScriptableBase
    {
        public int Count;

        public Timer Delay;

        public bool Firing { get; private set; }

        Transform attackOrigin;

        int count;

        public override void Attack(Transform origin)
        {
            attackOrigin = origin;
            count = 0;
            Firing = true;
        }

        public override void Tick()
        {
            base.Tick();

            Delay.Tick(Time.fixedDeltaTime);

            if (Firing && count < Count)
            {
                if (Delay.Ended)
                {
                    base.Attack(attackOrigin);
                    count++;
                    Delay.Reset();
                }
            }
            else
            {
                Firing = false;
            }
        }
    }
}
