using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItemBase : PetBehaviourBase
    {
        public const string EquipAction = "Equip";
        public const string UnequipAction = "Unequip";

        public IItemEquipper Parent { get; private set; }

        public ItemInstance Instance { get; private set; }

        public EquippableItemType Item => Instance.Type is EquippableItemType eq ? eq : null;

        public void Init(IItemEquipper parent)
        {
            Parent = parent;
            Owner = parent.GetRootOwner();
        }

        public event Action<string> OnStartAction;
        public void StartActionEvent(string actionId) => OnStartAction?.Invoke(actionId);

        public event Action OnUnequip;
        public event Action OnEquip;
        public event Action OnObjectEnable;
        public event Action OnObjectDisable;

        public readonly BooleanLock CanUnequip = new();

        public EquippedItemState CurrentState
        {
            get => currentState;
            protected set
            {
                currentState?.End();
                currentState = value;
                currentState?.Begin();
            }
        }
        private EquippedItemState currentState;

        public virtual void Equip(ItemInstance inst)
        {
            if (Instance != null)
                Instance.OnDestroy -= OnInstanceDestroyed;
            Instance = inst;
            Instance.OnDestroy += OnInstanceDestroyed;
            OnEquip?.Invoke();
            StartActionEvent(EquipAction);
        }

        public virtual void Unequip()
        {
            OnUnequip?.Invoke();
            StartActionEvent(UnequipAction);
        }

        /// <summary>
        /// Runs every tick where it tries to unequip; if it returns <b>true</b>, it will unequip.
        /// </summary>
        /// <returns>Whether the unequip is allowed.</returns>
        public virtual bool AttemptUnequip() => CanUnequip;

        protected virtual void Awake() { }
        protected virtual void FixedUpdate() => CurrentState?.Tick();
        protected virtual void Update() => CurrentState?.Frame();

        protected virtual void OnDestroy()
        {
            if (Instance != null)
                Instance.OnDestroy -= OnInstanceDestroyed;
        }

        protected virtual void OnEnable() => OnObjectEnable?.Invoke();
        protected virtual void OnDisable() => OnObjectDisable?.Invoke();

        protected virtual void OnInstanceDestroyed()
        {
            if (Parent.Current == this)
                Parent.ForceUnequip(true);
        }
    }

    public abstract class EquippedItemState
    {
        public EquippedItemBase Item { get; private set; }
        public virtual void Init(EquippedItemBase t) => Item = t;
        public abstract void Begin();
        public abstract void Tick();
        public abstract void Frame();
        public abstract void End();
    }

    public abstract class EquippedItemState<T> : EquippedItemState where T : EquippedItemBase
    {
        public new T Item => base.Item as T;
    }
}
