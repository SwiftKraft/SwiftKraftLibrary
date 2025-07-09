namespace Coherence.Toolkit
{
    using System;
    using UnityEngine.Events;

    [Serializable]
    public class UnityEventCountable : UnityEvent
    {
        private int runtimeListenersCount;

        public new void AddListener(UnityAction call)
        {
            base.AddListener(call);
            runtimeListenersCount++;
        }

        public new void RemoveListener(UnityAction call)
        {
            base.RemoveListener(call);
            runtimeListenersCount--;
        }

        public int RuntimeListenersCount => runtimeListenersCount;
        public int Count => runtimeListenersCount + GetPersistentEventCount();
    }

    [Serializable]
    public class UnityEventCountable<T0> : UnityEvent<T0>
    {
        private int runtimeListenersCount;

        public new void AddListener(UnityAction<T0> call)
        {
            base.AddListener(call);
            runtimeListenersCount++;
        }

        public new void RemoveListener(UnityAction<T0> call)
        {
            base.RemoveListener(call);
            runtimeListenersCount--;
        }

        public int RuntimeListenersCount => runtimeListenersCount;
        public int Count => runtimeListenersCount + GetPersistentEventCount();
    }

    [Serializable]
    public class UnityEventCountable<T0, T1> : UnityEvent<T0, T1>
    {
        private int runtimeListenersCount;

        public new void AddListener(UnityAction<T0, T1> call)
        {
            base.AddListener(call);
            runtimeListenersCount++;
        }

        public new void RemoveListener(UnityAction<T0, T1> call)
        {
            base.RemoveListener(call);
            runtimeListenersCount--;
        }

        public int RuntimeListenersCount => runtimeListenersCount;
        public int Count => runtimeListenersCount + GetPersistentEventCount();
    }

    [Serializable]
    public class UnityEventCountable<T0, T1, T2> : UnityEvent<T0, T1, T2>
    {
        private int runtimeListenersCount;

        public new void AddListener(UnityAction<T0, T1, T2> call)
        {
            base.AddListener(call);
            runtimeListenersCount++;
        }

        public new void RemoveListener(UnityAction<T0, T1, T2> call)
        {
            base.RemoveListener(call);
            runtimeListenersCount--;
        }

        public int RuntimeListenersCount => runtimeListenersCount;
        public int Count => runtimeListenersCount + GetPersistentEventCount();
    }

    [Serializable]
    public class UnityEventCountable<T0, T1, T2, T3> : UnityEvent<T0, T1, T2, T3>
    {
        private int runtimeListenersCount;

        public new void AddListener(UnityAction<T0, T1, T2, T3> call)
        {
            base.AddListener(call);
            runtimeListenersCount++;
        }

        public new void RemoveListener(UnityAction<T0, T1, T2, T3> call)
        {
            base.RemoveListener(call);
            runtimeListenersCount--;
        }

        public int RuntimeListenersCount => runtimeListenersCount;
        public int Count => runtimeListenersCount + GetPersistentEventCount();
    }
}
