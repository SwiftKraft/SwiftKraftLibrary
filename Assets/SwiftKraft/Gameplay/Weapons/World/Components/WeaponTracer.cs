using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponTracer : WeaponComponent
    {
        public Transform VisualOrigin;

        protected virtual void Awake() => Parent.OnAttack += OnAttack;

        protected virtual void OnDestroy() => Parent.OnAttack -= OnAttack;

        private void OnAttack(GameObject obj)
        {
            
        }
    }
}
