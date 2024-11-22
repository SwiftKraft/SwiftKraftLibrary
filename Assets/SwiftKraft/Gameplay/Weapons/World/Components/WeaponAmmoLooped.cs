using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAmmoLooped : WeaponAmmo
    {
        public int Amount;

        public int LoadedAmmo { get; private set; }

        protected override void Reload() => LoadedAmmo = 0;

        public override void EndReload() => LoadedAmmo = 0;

        public virtual void AddAmmo()
        {
            if (CanReload)
            {
                CurrentAmmo = Mathf.Clamp(CurrentAmmo + Amount, 0, MaxAmmo);
                LoadedAmmo += Amount;
            }
        }
    }
}
