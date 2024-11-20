using System;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponEquipper : WeaponComponentBlocker
    {
        public const string EquipAction = "Equip";

        public event Action OnStartEquip;
        public event Action OnEndEquip;

        protected override void Awake()
        {
            base.Awake();
            Parent.AddAction(EquipAction, StartEquip);
            Initialize();
        }

        protected virtual void OnEnable() => Initialize();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Parent.Actions.Remove(EquipAction);
        }

        public virtual void Initialize() => Parent.StartAction(EquipAction);

        public virtual bool StartEquip()
        {
            AttackDisabler.Active = true;
            OnStartEquip?.Invoke();
            return true;
        }

        public virtual void EndEquip()
        {
            AttackDisabler.Active = false;
            OnEndEquip?.Invoke();
        }
    }
}
