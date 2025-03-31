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
            LoadDelay.Reset();
            OnStartLoadEvent();
        }

        protected virtual void FixedUpdate()
        {
            LoadDelay.Tick(Time.fixedDeltaTime * ReloadSpeedMultiplier);

            if (Reloading && LoadDelay.Ended)
            {
                AddAmmo();
                if (Reloading)
                {
                    LoadDelay.Reset();
                    OnStartLoadEvent();
                }
            }
        }
    }
}
