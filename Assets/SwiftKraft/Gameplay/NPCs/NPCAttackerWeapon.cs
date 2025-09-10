using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Weapons;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCAttackerWeapon : NPCSingleTargetAttacker
    {
        public WeaponBase Weapon;

        public float Range = 30f;
        public float AttackDot = 0.7f;
        public Vector3 AttackDotAxis = Vector3.forward;

        protected ILookable Lookable;
        protected IItemEquipper ItemEquipper;

        protected override void Awake()
        {
            base.Awake();
            Lookable = Parent.GetComponent<ILookable>();
            ItemEquipper = Parent.GetComponent<IItemEquipper>();

            if ((Object)ItemEquipper != null)
                ItemEquipper.OnCurrentChanged += OnItemChanged;

            if (Weapon == null)
                Weapon = GetComponentInChildren<WeaponBase>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if ((Object)ItemEquipper != null)
                ItemEquipper.OnCurrentChanged -= OnItemChanged;
        }

        protected virtual void OnItemChanged(Inventory.Items.EquippedItem obj)
        {
            if (obj == null)
            {
                Weapon = null;
                return;
            }

            obj.TryGetComponent(out Weapon);
        }

        public override void Attack()
        {
            if (Weapon == null)
                return;

            Vector3 direction = (CurrentTarget.Value.position - Lookable.LookPoint.position).normalized;
            Lookable.WishLookRotation = Quaternion.LookRotation(direction, transform.up);

            if (Vector3.Dot(direction, Lookable.LookPoint.rotation * AttackDotAxis.normalized) >= AttackDot)
                Weapon.StartAction(WeaponBase.AttackAction);
        }
    }
}
