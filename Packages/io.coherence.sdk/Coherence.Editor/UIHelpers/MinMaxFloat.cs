// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;

    internal class MinMaxFloat
    {
        public float MaxValue { get; }
        public float MinValue { get; }

        public MinMaxFloat(float minValue, float maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public float Lerp(float amount, bool unclamped = false)
        {
            return unclamped ? Mathf.LerpUnclamped(MinValue, MaxValue, amount) : Mathf.Lerp(MinValue, MaxValue, amount);
        }

        public float InverseLerp(float amount)
        {
            return Mathf.InverseLerp(MinValue, MaxValue, amount);
        }
    }
}
