using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    [RequireComponent(typeof(FPSViewModelSway))]
    public class FPSViewModelSwayAim : RequiredDependencyComponent<FPSViewModelSway>
    {
        public WeaponAds TargetComponent { get; private set; }

        public ModifiableStatistic Multiplier;
        public ModifiableStatistic TiltMultiplier;

        ModifiableStatistic.Modifier modMult;
        ModifiableStatistic.Modifier modTilt;

        private void Awake()
        {
            TargetComponent = GetComponentInParent<WeaponAds>();
            modMult = Component.Multiplier.AddModifier();
            modTilt = Component.TiltMultiplier.AddModifier();

            modMult.Type = ModifiableStatistic.ModifierType.Multiplication;
            modTilt.Type = ModifiableStatistic.ModifierType.Multiplication;
        }

        private void Update()
        {
            modMult.Value = Mathf.Lerp(1f, Multiplier, TargetComponent.Aiming);
            modTilt.Value = Mathf.Lerp(1f, TiltMultiplier, TargetComponent.Aiming);
        }
    }
}
