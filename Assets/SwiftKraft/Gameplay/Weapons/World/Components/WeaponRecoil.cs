using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponRecoil : WeaponComponent
    {
        public Transform GetRecoilTransform() => RecoilTransformOverride != null ? RecoilTransformOverride : ((Object)RecoilTransform != null ? RecoilTransform.Transform : null);

        public Transform RecoilTransformOverride;
        public IRecoilTransform RecoilTransform { get; private set; }

        public ModifiableStatistic RecoilMultiplier = new();
        public ModifiableStatistic DecayMultiplier = new();

        public AnimationCurve DecayRate;
        public Accumulator Heat = new(Mathf.Infinity);

        public bool Smooth = false;

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
            Transform tr = GetRecoilTransform();
            if (tr != null)
            {
                if (Smooth && !Heat.CanDecay)
                    ApplyRecoil(tr);
                DecayRecoil(tr);
            }
        }

        protected abstract void DecayRecoil(Transform trans);
        protected abstract void ApplyRecoil(Transform trans);

        protected virtual void OnAttack(GameObject go)
        {
            Heat.Increment(1f);
            if (!Smooth)
                ApplyRecoil(GetRecoilTransform());
        }
    }
}
