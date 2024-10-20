using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// Used to accumulate and cap values, as well as handle decay, contains useful events. ~SwiftKraft
    /// </summary>
    [Serializable]
    public class Accumulator : ITickable, IValue<float>
    {
        public delegate void OnValueChanged(float prev, float curr);
        public delegate void OnValueZeroed();
        public delegate void OnValueMaxed();

        /// <summary>
        /// Called whenever the current value is changed, will not call again if set value is the same as the current value.
        /// </summary>
        public OnValueChanged OnChanged;
        /// <summary>
        /// Called whenever the current value reaches 0, will not call again if another set to 0 is called. 
        /// </summary>
        public OnValueZeroed OnZeroed;
        /// <summary>
        /// Called whenever the current value reaches MaxValue, will not call again if another set to MaxValue is called.
        /// </summary>
        public OnValueMaxed OnMaxed;

        /// <summary>
        /// The maximum CurrentValue can go up to.
        /// </summary>
        [field: SerializeField]
        public float MaxValue { get; set; }

        /// <summary>
        /// How much the value will decay per second.
        /// </summary>
        [field: SerializeField]
        public float DecayRate { get; set; }

        /// <summary>
        /// The current value, automatically capped between 0.0 and MaxValue.
        /// </summary>
        public float CurrentValue
        {
            get => currentValue;
            private set
            {
                float v = Mathf.Clamp(value, 0f, MaxValue);

                if (currentValue == v)
                    return;

                OnChanged?.Invoke(currentValue, v);
                currentValue = v;

                if (currentValue <= 0f)
                    OnZeroed?.Invoke();
                else if (currentValue >= MaxValue)
                    OnMaxed?.Invoke();
            }
        }

        /// <summary>
        /// Controls whether or not decaying is allowed, ignored by Decrement().
        /// </summary>
        [field: SerializeField]
        public bool CanDecay { get; set; }

        private float currentValue;

        /// <summary>
        /// No-arg constructor. Initializes CanDecay to true.
        /// </summary>
        public Accumulator() { CanDecay = true; }

        /// <summary>
        /// Initializes MaxValue.
        /// </summary>
        /// <param name="max">The amount for MaxValue</param>
        public Accumulator(float max) : this()
        {
            MaxValue = max;
        }

        /// <summary>
        /// Initializes MaxValue and CurrentValue.
        /// </summary>
        /// <param name="max">The amount for MaxValue</param>
        /// <param name="startingValue">The amount for CurrentValue</param>
        public Accumulator(float max, float startingValue) : this(max)
        {
            CurrentValue = startingValue;
        }

        /// <summary>
        /// Decays the current value, checks for CanDecay.
        /// </summary>
        /// <param name="deltaTime">Delta time of your update function. (ie. Update() -> Time.deltaTime, FixedUpdate() -> Time.fixedDeltaTime)</param>
        /// <returns>The current value.</returns>
        public float Tick(float deltaTime)
        {
            if (!CanDecay)
                return CurrentValue;

            return Decrement(deltaTime * DecayRate);
        }

        /// <summary>
        /// Increments the current value, automatically clamped between 0.0 and MaxValue.
        /// </summary>
        /// <param name="value">The amount you want to increment.</param>
        /// <returns>The current value.</returns>
        public float Increment(float value)
        {
            CurrentValue += value;

            return CurrentValue;
        }

        /// <summary>
        /// Decrements the current value, automatically clamped between 0.0 and MaxValue, ignores CanDecay.
        /// </summary>
        /// <param name="value">The amount you want to decrement.</param>
        /// <returns></returns>
        public float Decrement(float value)
        {
            CurrentValue -= value;

            return CurrentValue;
        }

        /// <summary>
        /// Sets the current value, automatically clamped between 0.0 and MaxValue.
        /// </summary>
        /// <param name="value">The amount you want to set.</param>
        public void Set(float value)
        {
            CurrentValue = value;

            return;
        }

        public float GetPercentage() => Mathf.InverseLerp(0f, MaxValue, CurrentValue);
    }
}