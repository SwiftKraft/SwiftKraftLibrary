using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttackSingle : WeaponAttackBase
    {
        public override void Attack(Transform origin)
        {
            if (!Parent.CanAttack)
                return;

            Parent.PreAttackEvent();
            GameObject[] go = Spawn(origin);
            Parent.AttackEvent(go);
        }
    }
}
