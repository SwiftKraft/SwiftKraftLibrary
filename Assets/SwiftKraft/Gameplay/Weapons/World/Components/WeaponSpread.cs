using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponSpread : WeaponComponent
    {
        public Transform SpreadTransform;

        public ModifiableStatistic SpreadMultiplier = new(1f);
        public ModifiableStatistic SpreadAimMultiplier = new(1f);

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

            Parent.OnPreSpawn += OnPreSpawn;
        }

        protected virtual void OnDestroy() => Parent.OnPreSpawn -= OnPreSpawn;

        protected virtual void OnPreSpawn() => ApplySpread();

        protected virtual float GetCurrentMultiplier() => Aim != null ? Mathf.Lerp(SpreadMultiplier, SpreadAimMultiplier, Aim.Aiming) : SpreadMultiplier;

        public virtual float GetSpread()
        {
            if (Aim != null && Recoil != null)
                return Mathf.Lerp(SpreadRecoil.EvaluateSafe(Recoil.Heat.CurrentValue), SpreadAimRecoil.EvaluateSafe(Recoil.Heat.CurrentValue), Aim.Aiming) * GetCurrentMultiplier();
            else if (Aim != null)
                return Mathf.Lerp(Spread, SpreadAim, Aim.Aiming) * GetCurrentMultiplier();
            else if (Recoil != null)
                return SpreadRecoil.EvaluateSafe(Recoil.Heat.CurrentValue) * GetCurrentMultiplier();
            return Spread * GetCurrentMultiplier();
        }

        public abstract void ApplySpread();
    }
}
