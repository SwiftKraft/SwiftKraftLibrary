using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAmmoCommunicator : AnimatorCommunicator<WeaponAmmo>
    {
        public string ParameterNameAmmo = "Ammo";
        public string ParameterNameReloading = "Reloading";
        public string ParameterNameReloadSpeed = "ReloadSpeedMultiplier";

        protected override void Awake()
        {
            base.Awake();
            ParentComponent.OnAmmoUpdated += OnAmmoUpdated;
            ParentComponent.OnReloadUpdated += OnReloadUpdated;
            ParentComponent.ReloadSpeedMultiplier.OnUpdate += OnReloadSpeedUpdate;
        }

        private void Start()
        {
            OnAmmoUpdated(ParentComponent.CurrentAmmo);
            OnReloadSpeedUpdate(ParentComponent.ReloadSpeedMultiplier);
        }

        private void OnEnable()
        {
            OnAmmoUpdated(ParentComponent.CurrentAmmo);
            OnReloadSpeedUpdate(ParentComponent.ReloadSpeedMultiplier);
        }

        private void OnDestroy()
        {
            ParentComponent.OnAmmoUpdated -= OnAmmoUpdated;
            ParentComponent.OnReloadUpdated -= OnReloadUpdated;
            ParentComponent.ReloadSpeedMultiplier.OnUpdate -= OnReloadSpeedUpdate;
        }

        private void OnReloadUpdated(bool obj) => Animator.SetBoolSafe(ParameterNameReloading, obj);

        private void OnReloadSpeedUpdate(float speed) => Animator.SetFloatSafe(ParameterNameReloadSpeed, speed);

        private void OnAmmoUpdated(int ammo) => Animator.SetFloatSafe(ParameterNameAmmo, ammo);
    }
}
