using SwiftKraft.Gameplay.Weapons;

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

        private void OnStartAction(string obj) => Animator.PlayAnimation(obj);
    }
}