using SwiftKraft.Gameplay.Common.FPS.ViewModels;
using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons {

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