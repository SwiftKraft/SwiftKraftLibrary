using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorUtilities : RequiredDependencyComponent<Animator>
    {
        public ViewModelAnimator ViewModel { get; private set; }

        public string VariantID = "Variant";

        private void Awake() => ViewModel = GetComponent<ViewModelAnimator>();

        public void SetVariant(int value) => Component.SetFloatSafe(VariantID, value);

        public void SetRandomVariant(int variants) => SetVariant(Random.Range(0, variants));
    }
}
