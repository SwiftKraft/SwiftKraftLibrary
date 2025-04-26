using SwiftKraft.Gameplay.Interfaces;
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

        protected ILookable Lookable;

        protected override void Awake()
        {
            base.Awake();
            Lookable = Parent.GetComponent<ILookable>();
            if (Weapon == null)
                Weapon = GetComponentInChildren<WeaponBase>();
        }

        public override void Attack()
        {
            Lookable.WishLookRotation = Quaternion.LookRotation((CurrentTarget.Value.position - Lookable.LookPoint.position).normalized, transform.up);
            Weapon.StartAction(WeaponBase.AttackAction);
        }
    }
}
