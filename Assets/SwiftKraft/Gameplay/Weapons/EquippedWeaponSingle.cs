using SwiftKraft.Gameplay.Inventory.Items;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class EquippedWeaponSingle : EquippedWeaponBase
    {
        public BasicAttack AttackState;
        public Idle IdleState = new();

        protected override void Awake()
        {
            base.Awake();
            AttackStateInstance = AttackState;
            IdleStateInstance = IdleState;
        }

        public class Idle : EquippedItemState<EquippedWeaponBase>
        {
            public override void Begin() { }

            public override void End() { }

            public override void Frame()
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                    Item.Attack();
            }

            public override void Tick() { }
        }
    }
}
