using SwiftKraft.Utils;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public abstract class EquippedItemDrawTime : EquippedItemBase
    {
        public EquippedItemWaitState EquipStateInstance { get; protected set; }
        public EquippedItemWaitState UnequipStateInstance { get; protected set; }
        public EquippedItemState IdleStateInstance { get; protected set; }

        protected virtual void Start()
        {
            EquipStateInstance.Init(this);
            UnequipStateInstance.Init(this);

            EquipStateInstance.OnEnd.AddListener(SetIdle);
        }

        public override void Equip(ItemInstance inst)
        {
            base.Equip(inst);
            CurrentState = EquipStateInstance;
        }

        public void SetIdle() => CurrentState = IdleStateInstance;

        public override bool AttemptUnequip()
        {
            if (CurrentState == EquipStateInstance || (CurrentState == UnequipStateInstance && !UnequipStateInstance.Ended))
                return false;

            if (UnequipStateInstance != null && UnequipStateInstance.WaitTimer.MaxValue > 0f && CurrentState != UnequipStateInstance)
            {
                CurrentState = UnequipStateInstance;
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public class EquippedItemWaitState : EquippedItemState<EquippedItemBase>
    {
        public Timer WaitTimer;
        public UnityEvent OnStart;
        public UnityEvent OnEnd;

        public bool Ended => WaitTimer.Ended;

        public override void Init(EquippedItemBase t)
        {
            base.Init(t);
            WaitTimer.Reset();
            WaitTimer.OnTimerEnd += OnTimerEnd;
        }
        public override void Begin()
        {
            WaitTimer.Reset();
            OnStart?.Invoke();
        }
        public override void End() { }
        public override void Frame() { }
        public override void Tick() => WaitTimer.Tick(Time.fixedDeltaTime);
        protected virtual void OnTimerEnd() => OnEnd?.Invoke();
    }
}
