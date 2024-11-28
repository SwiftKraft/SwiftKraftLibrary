using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Gameplay.Weapons.Interfaces;
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

        private void OnAttack(GameObject obj)
        {
            if (obj.TryGetComponent(out IVisualOrigin origin))
                origin.VisualOrigin = ViewModel == null ? VisualOrigin.position : ViewModel.Parent.MainCamera.ScreenToWorldPoint(ViewModel.Parent.ViewModelCamera.WorldToScreenPoint(VisualOrigin.position));
        }
    }
}
