namespace SwiftKraft.Gameplay.Weapons
{
    public class EquippedWeaponBasic : EquippedWeaponBase
    {
        public BasicAttack AttackState;

        protected override void Awake()
        {
            base.Awake();
            AttackStateInstance = AttackState;
        }
    }
}
