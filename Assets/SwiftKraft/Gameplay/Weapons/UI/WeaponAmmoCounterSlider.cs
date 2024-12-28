using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.Weapons.UI
{
    [RequireComponent(typeof(Slider))]
    public class WeaponAmmoCounterSlider : WeaponAmmoCounterBase<Slider>
    {
        protected override void OnAmmoUpdated(int amount)
        {
            Component.maxValue = Ammo.MaxAmmo;
            Component.value = amount;
        }
    }
}
