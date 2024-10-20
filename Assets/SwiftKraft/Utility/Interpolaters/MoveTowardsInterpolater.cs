using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// Uses MoveTowards to interpolate values.
    /// </summary>
    [Serializable]
    public class MoveTowardsInterpolater : Interpolater
    {
        /// <summary>
        /// The units per second.
        /// </summary>
        [field: SerializeField]
        public float Speed { get; set; }

        /// <summary>
        /// No-arg constructor. Speed defaults to 5f.
        /// </summary>
        public MoveTowardsInterpolater()
        {
            Speed = 5f;
        }

        public MoveTowardsInterpolater(float speed)
        {
            Speed = speed;
        }

        public override float Tick(float deltaTime)
        {
            CurrentValue = Mathf.MoveTowards(CurrentValue, MaxValue, Speed * deltaTime);

            InterpolationCheck();

            return CurrentValue;
        }
    }
}
