using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponUnequipperTimer : WeaponUnequipper
    {
        public Timer UnequipTimer;

        protected override void Awake()
        {
            base.Awake();
            UnequipTimer.OnTimerEnd += EndUnequip;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnequipTimer.OnTimerEnd -= EndUnequip;
        }

        protected virtual void FixedUpdate() => UnequipTimer.Tick(Time.fixedDeltaTime);

        public override bool StartUnequip()
        {
            UnequipTimer.Reset();
            return base.StartUnequip();
        }
    }
}
