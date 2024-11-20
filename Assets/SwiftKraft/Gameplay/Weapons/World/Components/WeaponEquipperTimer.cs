using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponEquipperTimer : WeaponEquipper
    {
        public Timer EquipTimer;

        protected override void Awake()
        {
            base.Awake();
            EquipTimer.OnTimerEnd += EndEquip;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EquipTimer.OnTimerEnd -= EndEquip;
        }

        protected virtual void FixedUpdate() => EquipTimer.Tick(Time.fixedDeltaTime);

        public override bool StartEquip()
        {
            EquipTimer.Reset();
            return base.StartEquip();
        }
    }
}
