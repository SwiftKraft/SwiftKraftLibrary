using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using UnityEngine;
using UnityEngine.Rendering;

namespace SwiftKraft.Gameplay.Weapons
{
    public class EquippedWeaponBase : EquippedItemDrawTime
    {
        public EquippedItemWaitState EquipState;
        public EquippedItemWaitState UnequipState;

        public GameObject ProjectilePrefab;

        public Transform ShootPoint;

        public virtual void Attack()
        {
            GameObject go = Instantiate(ProjectilePrefab, ShootPoint.position, ShootPoint.rotation);
            if (go.TryGetComponent(out IPet pet))
                pet.Owner = Owner;
        }

        public class AttackState : EquippedItemWaitState
        {
            public new EquippedWeaponBase Item => base.Item as EquippedWeaponBase;

            public override void Begin()
            {
                base.Begin();
                Item.Attack();
            }

            protected override void OnTimerEnd()
            {
                base.OnTimerEnd();
                if (Item != null)
                    Item.SetIdle();
            }
        }
    }
}
