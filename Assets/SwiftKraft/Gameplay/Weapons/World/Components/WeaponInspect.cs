namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponInspect : WeaponComponent
    {
        public const string InspectAction = "Inspect";

        WeaponAmmo ammo;

        protected virtual void Awake()
        {
            ammo = Parent.GetComponent<WeaponAmmo>();
            Parent.AddAction(InspectAction, Inspect);
        }

        protected virtual void OnDestroy() => Parent.Actions.Remove(InspectAction);

        public bool Inspect() => !Parent.Attacking && (ammo == null || !ammo.Reloading);
    }
}
