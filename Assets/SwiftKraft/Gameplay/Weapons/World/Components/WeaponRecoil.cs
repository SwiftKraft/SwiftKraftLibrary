using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponRecoil : WeaponComponent
    {
        public IRecoilTransform RecoilTransform { get; private set; }

        public float RecoilMultiplier = 1f;
        public float DecayMultiplier = 1f;

        public AnimationCurve DecayRate;
        public Accumulator Heat = new(Mathf.Infinity);

        protected virtual void Awake()
        {
            RecoilTransform = transform.root.GetComponentInChildren<IRecoilTransform>();
            Component.OnAttack += OnAttack;
        }

        protected virtual void OnDestroy() => Component.OnAttack -= OnAttack;

        protected virtual void FixedUpdate()
        {
            Heat.CanDecay = !Component.Attacking;
            Heat.Tick(DecayRate.Evaluate(Heat.CurrentValue) * DecayMultiplier * Time.fixedDeltaTime);
            if (RecoilTransform != null)
                DecayRecoil();
        }

        protected abstract void DecayRecoil();
        protected abstract void ApplyRecoil();

        protected virtual void OnAttack(GameObject go)
        {
            Heat.Increment(1f);
            ApplyRecoil();
        }
    }
}
