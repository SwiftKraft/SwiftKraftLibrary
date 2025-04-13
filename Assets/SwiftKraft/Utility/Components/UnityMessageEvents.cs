using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Utils
{
    public class UnityMessageEvents : MonoBehaviour
    {
        public UnityEvent OnAwakeEvent;
        public UnityEvent OnStartEvent;
        public UnityEvent OnEnableEvent;
        public UnityEvent OnDisableEvent;
        public UnityEvent OnDestroyEvent;

        private void OnEnable() => OnEnableEvent?.Invoke();
        private void OnDisable() => OnDisableEvent?.Invoke();
        private void OnDestroy() => OnDestroyEvent?.Invoke();
        private void Awake() => OnAwakeEvent?.Invoke();
        private void Start() => OnStartEvent?.Invoke();
    }
}
