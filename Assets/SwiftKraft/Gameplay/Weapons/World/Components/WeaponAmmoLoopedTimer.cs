using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoLoopedTimer : WeaponAmmoLooped
    {
        public Timer LoadDelay;

        public override bool Reloading => reloading;
        bool reloading;

        protected override void Reload()
        {
            base.Reload();
            reloading = true;
            LoadDelay.Reset();
            OnStartLoadEvent();
        }

        protected virtual void FixedUpdate()
        {
            LoadDelay.Tick(Time.fixedDeltaTime);

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

        public override void AddAmmo()
        {
            base.AddAmmo();
            if (CurrentAmmo >= MaxAmmo)
                reloading = false;
        }
    }
}
