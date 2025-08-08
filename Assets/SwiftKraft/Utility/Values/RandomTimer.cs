using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SwiftKraft.Utils
{
    [Serializable]
    public class RandomTimer : Timer
    {
        [field: SerializeField]
        public float MinValue { get; set; }
        public float CurrentRandom { get; private set; }

        public RandomTimer(float minValue, float maxValue, bool startEnded = true) : base(maxValue, startEnded)
        {
            MaxValue = maxValue;
            MinValue = minValue;
        }

        public override void Reset(float time)
        {
            CurrentRandom = time;
            CurrentValue = time;
        }

        public override void Reset()
        {
            CurrentRandom = Random.Range(MinValue, MaxValue);
            CurrentValue = CurrentRandom;
        }
    }
}
