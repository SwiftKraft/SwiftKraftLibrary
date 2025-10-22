using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponRecoil : RequiredDependencyComponent<EquippedWeaponBase>
    {
        public WeaponRecoil[] Additives;

        public Transform CurrentRecoilTransform => RecoilTransformOverride != null ? RecoilTransformOverride : (RecoilTransform != null ? RecoilTransform.transform : null);

        public Transform RecoilTransformOverride;
        public RecoilTransform RecoilTransform { get; private set; }

        public ModifiableStatistic RecoilMultiplier = new();
        public ModifiableStatistic DecayMultiplier = new();

        public AnimationCurve DecayRate;
        public Accumulator Heat = new(Mathf.Infinity);

        public Vector3 Position
        {
            get => modifier == null ? CurrentRecoilTransform.localPosition : modifier.Position;
            protected set
            {
                if (modifier != null)
                    modifier.Position = value;
                else
                    CurrentRecoilTransform.localPosition = value;
            }
        }

        public Quaternion Rotation
        {
            get => modifier == null ? CurrentRecoilTransform.localRotation : modifier.Rotation;
            protected set
            {
                if (modifier != null)
                    modifier.Rotation = value;
                else
                    CurrentRecoilTransform.localRotation = value;
            }
        }

        public bool Smooth = false;

        MultiModifyTransform.Modifier modifier;

        protected virtual void Awake()
        {
            RecoilTransform = transform.root.GetComponentInChildren<RecoilTransform>();

            if (RecoilTransform == null)
            {
                enabled = false;
                return;
            }

            Component.OnAttack += OnAttack;

            if (CurrentRecoilTransform.TryGetComponent(out MultiModifyTransform tr))
                modifier = tr.AddModifier();

            if (Additives.Length > 0)
                for (int i = 0; i < Additives.Length; i++)
                {
                    if (Additives[i] == this)
                        continue;

                    RecoilMultiplier.Additives.Add(Additives[i].RecoilMultiplier);
                    DecayMultiplier.Additives.Add(Additives[i].DecayMultiplier);
                }
        }

        protected virtual void OnDestroy()
        {
            Component.OnAttack -= OnAttack;
            modifier?.Remove();
        }

        protected virtual void FixedUpdate()
        {
            Heat.CanDecay = Component.CurrentState != Component.AttackStateInstance;
            Heat.Tick(DecayRate.EvaluateSafe(Heat.CurrentValue) * DecayMultiplier * Time.fixedDeltaTime);
            if (Smooth && !Heat.CanDecay)
                ApplyRecoil();
            DecayRecoil();
        }

        protected abstract void DecayRecoil();
        protected abstract void ApplyRecoil();

        protected virtual void OnAttack()
        {
            Heat.Increment(1f);
            if (!Smooth)
                ApplyRecoil();
        }
    }
}
