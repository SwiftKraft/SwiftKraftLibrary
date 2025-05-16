using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Utils
{
    public class BooleanEvent : MonoBehaviour
    {
        public UnityEvent OnTrue;
        public UnityEvent OnFalse;

        public void CallEvent(bool value)
        {
            if (value)
                OnTrue?.Invoke();
            else
                OnFalse?.Invoke();
        }
    }
}
