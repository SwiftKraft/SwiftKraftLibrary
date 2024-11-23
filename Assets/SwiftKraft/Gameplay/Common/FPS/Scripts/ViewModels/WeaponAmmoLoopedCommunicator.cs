using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAmmoLoopedCommunicator : AnimatorCommunicator<WeaponAmmoLooped>
    {
        public string ParameterName = "LoopedAmmo";

        private void Awake() => ParentComponent.OnLoopedAmmoChanged += OnLoopedAmmoUpdated;

        private void OnDestroy() => ParentComponent.OnLoopedAmmoChanged -= OnLoopedAmmoUpdated;

        private void OnLoopedAmmoUpdated(int ammo) => Animator.SetFloatSafe(ParameterName, ammo);
    }
}
