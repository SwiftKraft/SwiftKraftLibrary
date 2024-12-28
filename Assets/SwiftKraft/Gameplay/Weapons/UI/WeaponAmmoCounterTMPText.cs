using TMPro;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class WeaponAmmoCounterTMPText : WeaponAmmoCounterBase<TMP_Text>
    {
        protected override void OnAmmoUpdated(int amount) => Component.SetText(amount.ToString());
    }
}
