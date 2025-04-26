using UnityEngine;

namespace SwiftKraft.Utils
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorUtilities : RequiredDependencyComponent<Animator>
    {
        public string VariantID = "Variant";

        public void SetVariant(int value) => Component.SetFloatSafe(VariantID, value);

        public void SetRandomVariant(int variants) => SetVariant(Random.Range(0, variants));

        public void SetBoolTrue(string value) => Component.SetBoolSafe(value, true);
        public void SetBoolFalse(string value) => Component.SetBoolSafe(value, false);
    }
}
