using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class EquippedWeaponSingle : EquippedWeaponBase, IAmmo
    {
        public Shoot AttackState;
        public Idle IdleState = new();

        [field: SerializeField]
        public int MaxAmmo { get; set; } = 10;
        public Ammo AmmoData = new();

        public int CurrentAmmo => AmmoData != null ? AmmoData.CurrentAmmo : 0;

        protected override void Awake()
        {
            base.Awake();
            AttackStateInstance = AttackState;
            IdleStateInstance = IdleState;
        }

        public override void Equip(ItemInstance inst)
        {
            base.Equip(inst);
            Instance.TryData("Ammo", out AmmoData, n => n.CurrentAmmo = MaxAmmo);
        }

        public class Ammo : ItemDataBase
        {
            public int CurrentAmmo;
        }

        [Serializable]
        public class Shoot : BasicAttack
        {
            public new EquippedWeaponSingle Item => base.Item as EquippedWeaponSingle;

            public override void Begin()
            {
                base.Begin();
                Item.AmmoData.CurrentAmmo--;
            }
        }

        public class Idle : EquippedItemState<EquippedWeaponSingle>
        {
            public override void Begin() { }

            public override void End() { }

            public override void Frame()
            {
                if (Input.GetKeyDown(KeyCode.Mouse0) && Item.AmmoData.CurrentAmmo > 0)
                    Item.Attack();
            }

            public override void Tick() { }
        }
    }
}
