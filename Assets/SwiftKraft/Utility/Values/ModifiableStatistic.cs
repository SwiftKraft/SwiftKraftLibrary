using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    [Serializable]
    public class ModifiableStatistic
    {
        [field: SerializeField]
        public float BaseValue { get; private set; } = 1f;

        [field: SerializeField]
        public List<Modifier> Values { get; private set; } = new();

        public ModifiableStatistic() { }

        public ModifiableStatistic(float baseValue) { BaseValue = baseValue; }

        public float GetValue()
        {
            float value = BaseValue;
            foreach (Modifier mod in Values)
                value = mod.Modify(value);
            return value;
        }

        public static implicit operator float(ModifiableStatistic stat) => stat.GetValue();

        [Serializable]
        public class Modifier
        {
            [field: SerializeField]
            public float Value { get; set; }
            [field: SerializeField]
            public ModifierType Type { get; set; }

            public float Modify(float value) => Type switch
            {
                ModifierType.Addition => value + Value,
                ModifierType.Subtraction => value - Value,
                ModifierType.Division => value / Value,
                ModifierType.Multiplication => value * Value,
                _ => value,
            };
        }

        public enum ModifierType
        {
            Addition,
            Subtraction,
            Multiplication,
            Division
        }
    }
}
