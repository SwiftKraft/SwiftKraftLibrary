using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{

    [RequireComponent(typeof(WeaponAnimator))]
    public class WeaponAmmoLoopedAnimator : RequiredDependencyComponent<WeaponAmmoLooped>
    {
        public string AnimationID = "Reload";

        public WeaponAnimator Animator { get; private set; }

        private void Awake()
        {
            Animator = GetComponent<WeaponAnimator>();
            Component.OnStartLoad += OnStartLoad;
        }

        private void OnDestroy() => Component.OnStartLoad -= OnStartLoad;

        private void OnStartLoad() => Animator.PlayAnimation(AnimationID);
    }
}