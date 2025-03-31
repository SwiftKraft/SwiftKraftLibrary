using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    [Serializable]
    public class ModifiableStatistic : IOverrideParent
    {
        [field: SerializeField]
        public float BaseValue { get; private set; } = 1f;

        [field: SerializeField]
        public List<Modifier> Values { get; private set; } = new();

        public event Action<float> OnUpdate;
        public void UpdateValue() => OnUpdate?.Invoke(GetValue());

        public ModifiableStatistic() { }

        public ModifiableStatistic(float baseValue) { BaseValue = baseValue; }

        public float GetValue()
        {
            float value = BaseValue;
            foreach (Modifier mod in Values)
                value = mod.Modify(value);
            return value;
        }

        public Modifier AddModifier()
        {
            Modifier modifier = new(this);
            Values.Add(modifier);
            return modifier;
        }

        public void RemoveOverride(object target) => Values.Remove((Modifier)target);

        public static implicit operator float(ModifiableStatistic stat) => stat.GetValue();

        [Serializable]
        public class Modifier : OverrideBase<ModifiableStatistic>
        {


            
            public float Value
            {
                get => value; 
                set
                {
                    this.value = value;
                    Parent.UpdateValue();
                }
            }
            [SerializeField]
            float value;

            [field: SerializeField]
            public ModifierType Type { get; set; }

            public Modifier(ModifiableStatistic parent) : base(parent) { }

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
