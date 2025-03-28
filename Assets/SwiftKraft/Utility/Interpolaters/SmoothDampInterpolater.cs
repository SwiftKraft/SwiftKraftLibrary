using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// Uses SmoothDamp to interpolate values, deltaTime is not necessary to input for Tick().
    /// </summary>
    [Serializable]
    public class SmoothDampInterpolater : Interpolater
    {
        /// <summary>
        /// The smoothTime parameter for the SmoothDamp function.
        /// </summary>
        [field: SerializeField]
        public float SmoothTime { get; set; }

        public float SnapRange = 0.00001f;

        private float vel;

        /// <summary>
        /// No-arg constructor. SmoothTime defaults to 1f.
        /// </summary>
        public SmoothDampInterpolater() { SmoothTime = 1f; }

        public SmoothDampInterpolater(float smoothTime)
        {
            SmoothTime = smoothTime;
        }

        public override float Tick(float deltaTime)
        {
            if (SmoothTime > 0f && Mathf.Abs(CurrentValue - MaxValue) > SnapRange)
                CurrentValue = Mathf.SmoothDamp(CurrentValue, MaxValue, ref vel, SmoothTime, Mathf.Infinity, deltaTime);
            else
                CurrentValue = MaxValue;

            InterpolationCheck();

            return CurrentValue;
        }
    }
}
