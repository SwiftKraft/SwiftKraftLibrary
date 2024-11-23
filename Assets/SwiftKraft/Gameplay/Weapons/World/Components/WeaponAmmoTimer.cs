using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoTimer : WeaponAmmo
    {
        public override bool Reloading => !ReloadTimer.Ended;

        public Timer ReloadTimer;

        protected override void Awake()
        {
            base.Awake();
            ReloadTimer.OnTimerEnd += EndReload;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ReloadTimer.OnTimerEnd -= EndReload;
        }

        public virtual void FixedUpdate()
        {
            ReloadTimer.Tick(Time.fixedDeltaTime);
            CanShoot.Active = !ReloadTimer.Ended;
        }

        protected override void Reload() => ReloadTimer.Reset();
    }
}
