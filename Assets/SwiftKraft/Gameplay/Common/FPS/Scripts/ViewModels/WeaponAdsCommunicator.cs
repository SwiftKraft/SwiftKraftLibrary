using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAdsCommunicator : AnimatorCommunicator<WeaponAds>
    {
        public string ParameterName = "Ads";

        private void Update() => Animator.SetFloatSafe(ParameterName, ParentComponent.Aiming);
    }
}
