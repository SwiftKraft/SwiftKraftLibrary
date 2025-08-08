using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    [Serializable]
    public class TimerGroup : ITickable
    {
        [field: SerializeField]
        public List<Timer> Timers { get; set; } = new();
        public int CurrentTimer
        {
            get => _currentTimer;
            private set => _currentTimer = Mathf.Clamp(value, 0, Timers.Count - 1);
        }
        int _currentTimer;

        public Timer Current => Timers.Count > 0 ? Timers[CurrentTimer] : null;

        public virtual bool Ended => Timers.Count <= 0 || (CurrentTimer == Timers.Count - 1 && Timers[CurrentTimer].Ended);

        public virtual float Tick(float deltaTime)
        {
            if (Timers.Count <= 0)
                return 0f;

            float res = Timers[CurrentTimer].Tick(deltaTime);

            if (Timers[CurrentTimer].Ended)
                CurrentTimer++;

            return res;

        }

        public void Reset()
        {
            for (int i = 0; i < Timers.Count; i++)
                Timers[i].Reset();
        }
    }
}
