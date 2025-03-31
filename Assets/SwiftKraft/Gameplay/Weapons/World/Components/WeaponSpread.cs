using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponSpread : WeaponComponent
    {
        public Transform SpreadTransform;

        public ModifiableStatistic HipMultiplier = new(1f);
        public ModifiableStatistic AimMultiplier = new(1f);

        public float Spread;
        public float SpreadAim;
        public AnimationCurve SpreadRecoil;
        public AnimationCurve SpreadAimRecoil;

        public WeaponAds Aim;
        public WeaponRecoil Recoil;

        public Vector3 Position
        {
            get => modifier == null ? SpreadTransform.localPosition : modifier.Position;
            protected set
            {
                if (modifier != null)
                    modifier.Position = value;
                else
                    SpreadTransform.localPosition = value;
            }
        }

        public Quaternion Rotation
        {
            get => modifier == null ? SpreadTransform.localRotation : modifier.Rotation;
            protected set
            {
                if (modifier != null)
                    modifier.Rotation = value;
                else
                    SpreadTransform.localRotation = value;
            }
        }

        MultiModifyTransform.Modifier modifier;

        protected virtual void Awake()
        {
            if (SpreadTransform.TryGetComponent(out MultiModifyTransform tr))
                modifier = tr.AddModifier();

            Parent.OnPreAttack += OnPreAttack;
        }

        protected virtual void OnDestroy() => Parent.OnPreAttack -= OnPreAttack;

        protected virtual void OnPreAttack() => ApplySpread();

        protected virtual float GetCurrentMultiplier() => Aim != null ? Mathf.Lerp(HipMultiplier, AimMultiplier, Aim.Aiming) : HipMultiplier;

        public virtual float GetSpread()
        {
            if (Aim != null && Recoil != null)
                return Mathf.Lerp(SpreadRecoil.Evaluate(Recoil.Heat.CurrentValue), SpreadAimRecoil.Evaluate(Recoil.Heat.CurrentValue), Aim.Aiming) * GetCurrentMultiplier();
            else if (Aim != null)
                return Mathf.Lerp(Spread, SpreadAim, Aim.Aiming) * GetCurrentMultiplier();
            else if (Recoil != null)
                return SpreadRecoil.Evaluate(Recoil.Heat.CurrentValue) * GetCurrentMultiplier();
            return Spread * GetCurrentMultiplier();
        }

        public abstract void ApplySpread();
    }
}
