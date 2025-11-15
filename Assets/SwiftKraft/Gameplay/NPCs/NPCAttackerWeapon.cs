using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCAttackerWeapon : NPCSingleTargetAttacker
    {
        public EquippedWeaponBase EquippedWeapon;

        public float Range = 30f;
        public float AttackDot = 0.7f;
        public Vector3 AttackDotAxis = Vector3.forward;

        protected ILookable Lookable;

        protected override void Awake()
        {
            base.Awake();
            Lookable = Parent.GetComponent<ILookable>();
        }

        public override void Attack()
        {
            Vector3 direction = (CurrentTarget.Value.position - Lookable.LookPoint.position).normalized;
            Lookable.WishLookRotation = Quaternion.LookRotation(direction, transform.up);

            if (EquippedWeapon != null || this.TryGetComponentInChildren(out EquippedWeapon))
                EquippedWeapon.PerformAction(EquippedWeaponBase.AttackAction);
        }
    }
}
