using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoLoopedTimer : WeaponAmmoLooped
    {
        public Timer LoadDelay;

        protected override void Reload()
        {
            base.Reload();
            MidReload();
        }

        protected virtual void FixedUpdate()
        {
            LoadDelay.Tick(Time.fixedDeltaTime * ReloadSpeedMultiplier);

            if (Reloading && LoadDelay.Ended)
            {
                AddAmmo();
                if (Reloading)
                    MidReload();
            }
        }

        public override void MidReload() => LoadDelay.Reset();
    }
}
