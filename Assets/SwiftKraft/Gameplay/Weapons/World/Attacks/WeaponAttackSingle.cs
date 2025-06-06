using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttackSingle : WeaponAttackCooldown
    {
        public override void Attack()
        {
            Parent.PreAttackEvent();
            GameObject[] go = Spawn(CurrentAttackTransform);
            Parent.AttackEvent(go);
        }
    }
}
