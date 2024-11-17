using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAmmoCommunicator : AnimatorCommunicator<WeaponAmmo>
    {
        public string ParameterName = "Ammo";

        private void Awake() => ParentComponent.OnAmmoUpdated += OnAmmoUpdated;

        private void OnDestroy() => ParentComponent.OnAmmoUpdated -= OnAmmoUpdated;

        private void OnAmmoUpdated(int ammo) => Animator.SetFloatSafe(ParameterName, ammo);
    }
}
