using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Weapons;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCAttackerWeapon : NPCSingleTargetAttacker
    {
        //public WeaponBase Weapon;

        public float Range = 30f;
        public float AttackDot = 0.7f;
        public Vector3 AttackDotAxis = Vector3.forward;

        protected ILookable Lookable;

        protected override void Awake()
        {
            base.Awake();
            Lookable = Parent.GetComponent<ILookable>();
            //if (Weapon == null)
            //    Weapon = GetComponentInChildren<WeaponBase>();
        }

        public override void Attack()
        {
            Vector3 direction = (CurrentTarget.Value.position - Lookable.LookPoint.position).normalized;
            Lookable.WishLookRotation = Quaternion.LookRotation(direction, transform.up);

            //if (Vector3.Dot(direction, Lookable.LookPoint.rotation * AttackDotAxis.normalized) >= AttackDot)
            //    Weapon.StartAction(WeaponBase.AttackAction);
        }
    }
}
