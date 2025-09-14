using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class Firearm : EquippedWeaponBase, IAmmo
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

        public class Reload : EquippedItemState<Firearm>
        {
            public override void Begin()
            {

            }

            public override void End()
            {

            }

            public override void Frame()
            {

            }

            public override void Tick()
            {

            }
        }

        [Serializable]
        public class Shoot : BasicAttack
        {
            public new Firearm Item => base.Item as Firearm;

            public override void Begin()
            {
                base.Begin();
                Item.AmmoData.CurrentAmmo--;
            }
        }

        public class Idle : EquippedItemState<Firearm>
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
