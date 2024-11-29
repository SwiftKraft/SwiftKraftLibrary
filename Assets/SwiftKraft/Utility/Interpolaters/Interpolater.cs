using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// Base class for interpolated values.
    /// </summary>
    [Serializable]
    public abstract class Interpolater : ITickable, IValue<float>
    {
        public delegate void OnInterpolateFinish();

        /// <summary>
        /// The value to interpolate to.
        /// </summary>
        [field: SerializeField]
        public float MaxValue { get; set; }

        /// <summary>
        /// The current value that is being interpolated.
        /// </summary>
        public float CurrentValue { get; set; }

        /// <summary>
        /// Called when the value stops interpolating.
        /// </summary>
        public OnInterpolateFinish OnFinish;

        /// <summary>
        /// Whether or not the value is currently being interpolated.
        /// </summary>
        public bool Interpolating { get; protected set; }

        /// <summary>
        /// Updates the interpolation.
        /// </summary>
        /// <param name="deltaTime">Delta time of your update function. (ie. Update() -> Time.deltaTime, FixedUpdate() -> Time.fixedDeltaTime)</param>
        /// <returns>The value after ticking.</returns>
        public abstract float Tick(float deltaTime);

        /// <summary>
        /// Checking if the interpolation has ended and updates the Interpolating boolean.
        /// </summary>
        protected void InterpolationCheck()
        {
            if (Interpolating && CurrentValue == MaxValue)
                OnFinish?.Invoke();

            Interpolating = CurrentValue != MaxValue;
        }

        public float GetPercentage() => Mathf.InverseLerp(0f, MaxValue, CurrentValue);
    }
}
