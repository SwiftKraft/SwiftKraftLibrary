using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [RequireComponent(typeof(EquippedWeaponBase))]
    public abstract class WeaponSpread : RequiredDependencyComponent<EquippedWeaponBase>
    {
        public const string SpreadAmountName = "Spread_BaseAmount";
        public const string SpreadMultiplierName = "Spread_Multiplier";

        public ModifiableStatistic BaseAmount = new();
        public readonly ModifiableStatistic Multiplier = new(1f);

        public Transform ModifyTransform;

        public float Current => BaseAmount * Multiplier;

        protected Vector3 OriginalPosition { get; set; }
        protected Quaternion OriginalRotation { get; set; }

        protected virtual void Awake()
        {
            if (ModifyTransform == null)
            {
                enabled = false;
                return;
            }

            OriginalPosition = ModifyTransform.localPosition;
            OriginalRotation = ModifyTransform.localRotation;

            Component.OnAttacking += OnAttacking;

            Component.ExposedStats.Add(SpreadAmountName, BaseAmount);
            Component.ExposedStats.Add(SpreadMultiplierName, Multiplier);
        }

        protected virtual void OnDestroy() => Component.OnAttacking -= OnAttacking;

        protected virtual void OnAttacking()
        {
            ModifyTransform.SetLocalPositionAndRotation(OriginalPosition, OriginalRotation);
            Randomize(ModifyTransform);
        }

        public abstract void Randomize(Transform target);
    }
}
