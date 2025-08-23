using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.Characters
{
    [RequireComponent(typeof(Animator))]
    public class CharacterGroundedAnimator : RequiredDependencyComponent<Animator>
    {
        public string ParameterName = "Grounded";
        public float SmoothTime = 0.1f;

        public IGroundable Groundable { get; private set; }

        float vel;
        float cur;

        private void Awake() => Groundable = GetComponentInParent<IGroundable>();

        private void Update()
        {
            cur = Mathf.SmoothDamp(cur, Groundable.IsGrounded ? 1f : 0f, ref vel, SmoothTime);
            Component.SetFloatSafe(ParameterName, cur);
        }
    }
}
