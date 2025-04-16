using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Weapons;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponTracer : WeaponComponent
    {
        public ViewModel ViewModel { get; private set; }
        public Transform VisualOrigin;

        protected virtual void Awake()
        {
            ViewModel = GetComponent<ViewModel>();
            Parent.OnAttack += OnAttack;
        }

        protected virtual void OnDestroy() => Parent.OnAttack -= OnAttack;

        private void OnAttack(GameObject[] obj)
        {
            if (VisualOrigin == null)
                return;

            foreach (GameObject go in obj)
            {
                if (go.TryGetComponent(out IVisualOrigin origin))
                    origin.VisualOrigin = ViewModel == null ? VisualOrigin.position : ViewModel.Parent.MainCamera.ScreenToWorldPoint(ViewModel.Parent.ViewModelCamera.WorldToScreenPoint(VisualOrigin.position));
            }
        }
    }
}
