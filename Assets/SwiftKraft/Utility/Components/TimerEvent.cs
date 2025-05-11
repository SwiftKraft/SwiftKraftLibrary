using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Utils
{
    public class TimerEvent : MonoBehaviour
    {
        public UnityEvent Execute;

        public Timer Timer;

        private void Awake() => Timer.Reset();

        private void FixedUpdate()
        {
            Timer.Tick(Time.fixedDeltaTime);
            if (Timer.Ended)
            {
                Timer.Reset();
                Execute?.Invoke();
            }
        }
    }
}
