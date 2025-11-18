using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
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

            Component.OnAttack += OnAttack;

            Component.ExposedStats.Add(SpreadAmountName, BaseAmount);
        }

        protected virtual void OnDestroy() => Component.OnAttack -= OnAttack;

        protected virtual void OnAttack()
        {
            ModifyTransform.SetLocalPositionAndRotation(OriginalPosition, OriginalRotation);
            Randomize(ModifyTransform);
        }

        public abstract void Randomize(Transform target);
    }
}
