using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// Uses Lerp to interpolate values, snaps to TargetValue when difference is <= SnapDifference.
    /// </summary>
    [Serializable]
    public class LerpInterpolater : Interpolater
    {
        /// <summary>
        /// The t parameter for the Lerp function, multiplied by delta time.
        /// </summary>
        [field: SerializeField]
        public float LerpValue { get; set; }

        /// <summary>
        /// How small of a difference in value should the CurrentValue snap to TargetValue.
        /// </summary>
        [field: SerializeField]
        public float SnapDifference { get; set; }

        /// <summary>
        /// No-arg constructor. LerpValue defaults to 100f, SnapDifference defaults to 0.01f.
        /// </summary>
        public LerpInterpolater()
        {
            LerpValue = 100f;
            SnapDifference = 0.01f;
        }

        /// <summary>
        /// SnapDifference defaults to 0.01f.
        /// </summary>
        /// <param name="lerpValue"></param>
        public LerpInterpolater(float lerpValue)
        {
            LerpValue = lerpValue;
            SnapDifference = 0.01f;
        }

        public LerpInterpolater(float lerpValue, float snapDifference)
        {
            LerpValue = lerpValue;
            SnapDifference = snapDifference;
        }

        public override float Tick(float deltaTime)
        {
            CurrentValue = Calculate(deltaTime);

            if (Mathf.Abs(CurrentValue - MaxValue) <= SnapDifference)
                CurrentValue = MaxValue;

            InterpolationCheck();

            return CurrentValue;
        }

        protected virtual float Calculate(float deltaTime) => Mathf.Lerp(CurrentValue, MaxValue, Mathf.Pow(0.5f, LerpValue * deltaTime));
    }
}
