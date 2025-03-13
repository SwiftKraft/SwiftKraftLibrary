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

        public Vector3 Position
        {
            get => modifier == null ? GetRecoilTransform().localPosition : modifier.Position;
            protected set
            {
                if (modifier != null)
                    modifier.Position = value;
                else
                    GetRecoilTransform().localPosition = value;
            }
        }

        public Quaternion Rotation
        {
            get => modifier == null ? GetRecoilTransform().localRotation : modifier.Rotation;
            protected set
            {
                if (modifier != null)
                    modifier.Rotation = value;
                else
                    GetRecoilTransform().localRotation = value;
            }
        }

        public bool Smooth = false;

        MultiModifyTransform.Modifier modifier;

        protected virtual void Awake()
        {
            RecoilTransform = transform.root.GetComponentInChildren<IRecoilTransform>();
            Component.OnAttack += OnAttack;

            if (GetRecoilTransform().TryGetComponent(out MultiModifyTransform tr))
                modifier = tr.AddModifier();
        }

        protected virtual void OnDestroy()
        {
            Component.OnAttack -= OnAttack;
            modifier?.Remove();
        }

        protected virtual void FixedUpdate()
        {
            Heat.CanDecay = !Component.Attacking;
            Heat.Tick(DecayRate.Evaluate(Heat.CurrentValue) * DecayMultiplier * Time.fixedDeltaTime);
            if (Smooth && !Heat.CanDecay)
                ApplyRecoil();
            DecayRecoil();
        }

        protected abstract void DecayRecoil();
        protected abstract void ApplyRecoil();

        protected virtual void OnAttack(GameObject go)
        {
            Heat.Increment(1f);
            if (!Smooth)
                ApplyRecoil();
        }
    }
}
