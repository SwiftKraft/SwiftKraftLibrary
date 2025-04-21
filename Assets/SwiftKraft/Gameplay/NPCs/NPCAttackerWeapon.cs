using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Gameplay.Weapons;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCAttackerWeapon : NPCSingleTargetAttacker
    {
        public WeaponBase Weapon;

        public float Range = 30f;

        protected MotorBase Motor;

        protected override void Awake()
        {
            base.Awake();
            Motor = Parent.GetComponent<MotorBase>();
            if (Weapon == null)
                Weapon = GetComponentInChildren<WeaponBase>();
        }

        public override void Attack()
        {
            Motor.WishLookPosition = CurrentTarget.Value.position;
            Weapon.StartAction(WeaponBase.AttackAction);
        }
    }
}
