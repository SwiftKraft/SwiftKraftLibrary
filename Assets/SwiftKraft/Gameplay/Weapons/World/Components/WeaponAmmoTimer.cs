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
            ReloadTimer.OnTimerEnd += End;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ReloadTimer.OnTimerEnd -= End;
        }

        public virtual void FixedUpdate()
        {
            ReloadTimer.Tick(Time.fixedDeltaTime);
            CanShoot.Active = !ReloadTimer.Ended;
        }

        private void End() => EndReload(true);

        protected override void Reload() => ReloadTimer.Reset();
    }
}
