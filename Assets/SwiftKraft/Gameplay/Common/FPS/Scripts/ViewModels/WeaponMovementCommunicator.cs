using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponMovementCommunicator : AnimatorCommunicator<WeaponMovement>
    {
        public string ParameterName = "Movement";

        private void Update() => Animator.SetFloatSafe(ParameterName, ParentComponent.State);
    }
}
