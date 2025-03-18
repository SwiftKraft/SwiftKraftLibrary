using SwiftKraft.Gameplay.Weapons;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAnimator : WeaponComponent
    {
        public ViewModelAnimator Animator { get; private set; }

        private void Awake()
        {
            Animator = GetComponentInChildren<ViewModelAnimator>();

            if (Animator == null)
                return;

            Component.OnStartAction += OnStartAction;
        }

        private void OnDestroy() => Component.OnStartAction -= OnStartAction;

        private void OnStartAction(string obj)
        {
            PlayAnimation(obj);
            Debug.Log(obj);
        }

        public void PlayAnimation(string id) => Animator.PlayAnimation(id);
    }
}
