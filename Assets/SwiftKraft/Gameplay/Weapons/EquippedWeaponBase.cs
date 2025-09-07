using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class EquippedWeaponBase : EquippedItemDrawTime
    {
        public EquippedItemWaitState EquipState;
        public EquippedItemWaitState UnequipState;

        public EquippedItemState AttackStateInstance { get; protected set; }

        protected override void Awake()
        {
            base.Awake();
            EquipStateInstance = EquipState;
            UnequipStateInstance = UnequipState;
        }

        public virtual void Attack() => CurrentState = AttackStateInstance;

        [Serializable]
        public class BasicAttack : EquippedItemWaitState
        {
            public GameObject ProjectilePrefab;
            public Transform ShootPoint;

            public new EquippedWeaponBase Item => base.Item as EquippedWeaponBase;

            public UnityEvent OnAttack;

            public override void Begin()
            {
                base.Begin();
                SpawnProjectile();
                OnAttack?.Invoke();
            }

            protected override void OnTimerEnd()
            {
                base.OnTimerEnd();
                if (Item != null)
                    Item.SetIdle();
            }

            public virtual void SpawnProjectile()
            {
                GameObject go = Instantiate(ProjectilePrefab, ShootPoint.position, ShootPoint.rotation);
                if (go.TryGetComponent(out IPet pet))
                    pet.Owner = Item.Owner;
            }
        }
    }
}
