namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponUnequipper : WeaponComponentBlocker // rework this to use items
    {
        public const string UnequipAction = "Unequip";

        protected override void Awake()
        {
            base.Awake();
            Parent.AddAction(UnequipAction, StartUnequip);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Parent.Actions.Remove(UnequipAction);
        }

        public virtual bool StartUnequip()
        {
            AttackDisabler.Active = true;
            return true;
        }

        public virtual void EndUnequip()
        {
            AttackDisabler.Active = false;
            gameObject.SetActive(false);
        }
    }
}