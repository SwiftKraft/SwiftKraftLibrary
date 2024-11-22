using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAmmoCommunicator : AnimatorCommunicator<WeaponAmmo>
    {
        public string ParameterNameAmmo = "Ammo";
        public string ParameterNameReloading = "Reloading";

        private void Awake() => ParentComponent.OnAmmoUpdated += OnAmmoUpdated;

        private void OnDestroy() => ParentComponent.OnAmmoUpdated -= OnAmmoUpdated;

        private void Update() => Animator.SetBoolSafe(ParameterNameReloading, ParentComponent.Reloading);

        private void OnAmmoUpdated(int ammo) => Animator.SetFloatSafe(ParameterNameAmmo, ammo);
    }
}
