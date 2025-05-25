using System.Linq;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAnimator : WeaponComponent
    {
        public ViewModelAnimator Animator { get; private set; }

        public string[] EventOverrides;

        private void Awake()
        {
            Animator = GetComponentInChildren<ViewModelAnimator>();

            if (Animator == null)
                return;

            Component.OnStartAction += OnStartAction;
            Component.OnEvent += OnEvent;
        }

        private void OnDestroy()
        {
            Component.OnStartAction -= OnStartAction;
            Component.OnEvent -= OnEvent;
        }

        private void OnEvent(string obj)
        {
            if (EventOverrides.Contains(obj))
                PlayAnimation(obj);
        }

        private void OnStartAction(string obj)
        {
            if (!EventOverrides.Contains(obj))
                PlayAnimation(obj);
        }

        public void PlayAnimation(string id) => Animator.PlayAnimation(id);
    }
}
