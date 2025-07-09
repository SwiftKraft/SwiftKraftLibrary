// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    public abstract class Observable<T>
    {
        public T Value { get; private set; }

        internal event Action<T, T> OnValueUpdated;

        protected Observable(T initialValue)
        {
            Value = initialValue;
        }

        public void UpdateValue(T newValue)
        {
            var oldValue = Value;
            Value = newValue;

            OnValueUpdated?.Invoke(oldValue, newValue);
        }
    }
}

