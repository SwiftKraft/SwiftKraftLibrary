namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponInspect : WeaponComponent
    {
        public const string InspectAction = "Inspect";

        protected virtual void Awake() => Parent.AddAction(InspectAction, Inspect);

        protected virtual void OnDestroy() => Parent.Actions.Remove(InspectAction);

        public bool Inspect() => !Parent.Attacking;
    }
}
