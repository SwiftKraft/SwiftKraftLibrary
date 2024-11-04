namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAmmo : WeaponBase
    {
        public int MaxAmmo { get; set; }
        public int CurrentAmmo { get; set; }

        public bool TryUseAmmo(int ammo = 1)
        {
            if (CurrentAmmo <= 0)
                return false;

            CurrentAmmo -= ammo;
            return true;
        }
    }
}
