using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Gameplay.Projectiles;
using SwiftKraft.Utils;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class EquippedWeaponBase : EquippedItemDrawTime
    {
        public const string AttackAction = "Attack";

        public readonly Blackboard<ModifiableStatistic> ExposedStats = new();

        public EquippedItemWaitState EquipState;
        public EquippedItemWaitState UnequipState;

        public EquippedItemState AttackStateInstance { get; protected set; }

        public event Action OnAttack;
        public event Action OnAttacking;

        protected override void Awake()
        {
            base.Awake();

            EquipStateInstance = EquipState;
            UnequipStateInstance = UnequipState;
            RegisterAction(AttackAction, Attack);
        }

        protected override void Start()
        {
            base.Start();
            AttackStateInstance?.Init(this);
        }

        public virtual bool Attack()
        {
            OnAttacking?.Invoke();
            CurrentState = AttackStateInstance;
            OnAttack?.Invoke();
            return true;
        }

        [Serializable]
        public class BasicAttack : EquippedItemWaitState
        {
            public float BaseDamage = 25f;

            public GameObject ProjectilePrefab;
            public Transform ShootPoint;

            public float QueueTime = 0.2f;

            public new EquippedWeaponBase Item => base.Item as EquippedWeaponBase;

            public UnityEvent OnAttack;

            protected BooleanLock.Lock canUnequip;
            protected bool queued;

            public override void Init(EquippedItemBase t)
            {
                base.Init(t);
                canUnequip = Item.CanUnequip.AddLock();
                canUnequip.Active = false;
            }

            public override void Begin()
            {
                base.Begin();
                SpawnProjectile();
                OnAttack?.Invoke();
                canUnequip.Active = true;
            }

            public override void Frame()
            {
                base.Frame();
                if (WaitTimer.CurrentValue <= QueueTime && CheckQueue())
                    queued = true;
            }

            public override void End()
            {
                base.End();
                canUnequip.Active = false;
            }

            protected override void OnTimerEnd()
            {
                base.OnTimerEnd();
                if (Item != null)
                {
                    Item.SetIdle();
                    if (queued)
                    {
                        Item.Attack();
                        queued = false;
                    }
                }
            }

            public virtual bool CheckQueue() => false;

            public virtual void SpawnProjectile()
            {
                GameObject go = Instantiate(ProjectilePrefab, ShootPoint.position, ShootPoint.rotation);

                if (go.TryGetComponent(out ProjectileBase proj))
                    proj.BaseDamage = BaseDamage;

                if (go.TryGetComponent(out IPet pet))
                    pet.Owner = Item.Owner;

                if (go.TryGetComponent(out IVisualOrigin origin) && (Item is IVisualOrigin source || Item.TryGetComponent(out source)))
                    origin.VisualOrigin = source.VisualOrigin;
            }
        }
    }
}
