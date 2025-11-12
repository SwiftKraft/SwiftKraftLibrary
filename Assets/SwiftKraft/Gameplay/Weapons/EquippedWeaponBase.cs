using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class EquippedWeaponBase : EquippedItemDrawTime
    {
        public readonly Dictionary<string, ModifiableStatistic> ExposedStats = new();

        public EquippedItemWaitState EquipState;
        public EquippedItemWaitState UnequipState;

        public EquippedItemState AttackStateInstance { get; protected set; }

        public event Action OnAttack;

        protected override void Awake()
        {
            base.Awake();

            EquipStateInstance = EquipState;
            UnequipStateInstance = UnequipState;
        }

        protected override void Start()
        {
            base.Start();
            AttackStateInstance?.Init(this);
        }

        public bool ExposeStat(string key, ModifiableStatistic stat)
        {
            if (ExposedStats.ContainsKey(key))
                return false;

            ExposedStats.Add(key, stat);
            return true;
        }

        public bool ConcealStat(string key) => ExposedStats.Remove(key);

        public ModifiableStatistic GetStat(string key) => ExposedStats.ContainsKey(key) ? ExposedStats[key] : null;

        public bool TryGetStat(string key, out ModifiableStatistic stat)
        {
            stat = GetStat(key);
            return stat != null;
        }

        public virtual void Attack()
        {
            if (CurrentState != IdleStateInstance)
                return;
            CurrentState = AttackStateInstance;
            OnAttack?.Invoke();
        }

        [Serializable]
        public class BasicAttack : EquippedItemWaitState
        {
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

                if (go.TryGetComponent(out IPet pet))
                    pet.Owner = Item.Owner;

                if (go.TryGetComponent(out IVisualOrigin origin) && (Item is IVisualOrigin source || Item.TryGetComponent(out source)))
                    origin.VisualOrigin = source.VisualOrigin;
            }
        }
    }
}
