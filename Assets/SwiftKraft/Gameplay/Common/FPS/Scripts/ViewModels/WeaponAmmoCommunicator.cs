using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAmmoCommunicator : AnimatorCommunicator<WeaponAmmo>
    {
        public string ParameterNameAmmo = "Ammo";
        public string ParameterNameReloading = "Reloading";

        private void Awake()
        {
            ParentComponent.OnAmmoUpdated += OnAmmoUpdated;
            ParentComponent.OnReloadUpdated += OnReloadUpdated;
        }

        private void Start() => OnAmmoUpdated(ParentComponent.CurrentAmmo);

        private void OnDestroy()
        {
            ParentComponent.OnAmmoUpdated -= OnAmmoUpdated;
            ParentComponent.OnReloadUpdated -= OnReloadUpdated;
        }

        private void OnReloadUpdated(bool obj) => Animator.SetBoolSafe(ParameterNameReloading, obj);

        private void OnAmmoUpdated(int ammo) => Animator.SetFloatSafe(ParameterNameAmmo, ammo);
    }
}
