using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAmmoLooped : WeaponAmmo
    {
        public int Amount = 1;

        protected override void Reload() => AddAmmo();

        public virtual void MidReload()
        {
            if (CanReload)
                AddAmmo();
        }

        public void AddAmmo() => CurrentAmmo = Mathf.Clamp(CurrentAmmo + Amount, 0, MaxAmmo);

        public override void EndReload() { }
    }
}
